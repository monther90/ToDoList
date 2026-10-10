using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Task.Data;
using Task.Models;

namespace Task.Controllers
{
    public class TodoItemController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly string _uploadFolder;

        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public TodoItemController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _uploadFolder = Path.Combine(environment.WebRootPath, "App Data", "Uploads");
            Directory.CreateDirectory(_uploadFolder); 

        }


        [HttpGet]
        public IActionResult Index(DateTime? searchDate, int page = 1)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid date or page.");
            }

            const int pageSize = 5;

            var query = _context.TodoItem.AsNoTracking();

            if (searchDate.HasValue)
            {
                var date = searchDate.Value.Date;

                query = query.Where(t => t.DeadLine.Value.Date == date);
            }

            int totalTasks = query.Count();

            int totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalTasks / (double)pageSize));

            page = Math.Clamp(page, 1, totalPages);

            var tasks = query
                .OrderBy(t => t.DeadLine)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");

            return View(tasks);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TodoItem model, IFormFile? file)
        {
            if (file != null)
            {
                if (file.Length > MaxFileSize)
                {
                    ModelState.AddModelError("file","The file size must be 50 MB or less");
                }

                string originalName = Path.GetFileName(
                    file.FileName.Replace('\\', '/'));

                if (originalName.Length > 255)
                {
                    ModelState.AddModelError("file","The file name is long");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var item = new TodoItem
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                DeadLine = model.DeadLine
            };

            string? savedPath = null;

            try
            {
                if (file != null)
                {
                    item.FileName = Path.GetFileName(
                        file.FileName.Replace('\\', '/'));

                    item.StoredFileName = Guid.NewGuid().ToString("N");

                    savedPath = Path.Combine(
                        _uploadFolder,
                        item.StoredFileName);

                    using (var stream = new FileStream(
                        savedPath,
                        FileMode.CreateNew))
                    {
                        file.CopyTo(stream);
                    }
                }

                _context.TodoItem.Add(item);
                _context.SaveChanges();
            }
            catch
            {
                if (savedPath != null && System.IO.File.Exists(savedPath))
                {
                    System.IO.File.Delete(savedPath);
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Download(int id)
        {
            var item = _context.TodoItem
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (item == null ||
                string.IsNullOrWhiteSpace(item.StoredFileName))
            {
                return NotFound("Task does not found.");
            }

            string filePath = Path.Combine(
                _uploadFolder,
                Path.GetFileName(item.StoredFileName));

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("The file does not exist on the server");
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(
                filePath,
                "application/octet-stream",
                item.FileName ?? "attachment");
        }
         
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _context.TodoItem.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, TodoItem model, IFormFile? file)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var item = _context.TodoItem.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            if (file != null)
            {
                if (file.Length > MaxFileSize)
                {
                    ModelState.AddModelError("file","The file size must be 50 MB or less");
                }

                string originalName = Path.GetFileName(
                    file.FileName.Replace('\\', '/'));

                if (originalName.Length > 255)
                {
                    ModelState.AddModelError("file","The file name is long");
                }
            }

            if (!ModelState.IsValid)
            {
               
                model.FileName = item.FileName;
                model.StoredFileName = item.StoredFileName;

                return View(model);
            }

            string? oldStoredFileName = item.StoredFileName;
            string? newFilePath = null;

            item.Title = model.Title.Trim();
            item.Description = model.Description?.Trim();
            item.DeadLine = model.DeadLine;

            try
            {
                
                if (file != null)
                {
                    string newStoredFileName = Guid.NewGuid().ToString("N");

                    newFilePath = Path.Combine(
                        _uploadFolder,
                        newStoredFileName);

                    using (var stream = new FileStream(
                        newFilePath,
                        FileMode.CreateNew))
                    {
                        file.CopyTo(stream);
                    }

                    item.FileName = Path.GetFileName(
                        file.FileName.Replace('\\', '/'));

                    item.StoredFileName = newStoredFileName;
                }

                _context.SaveChanges();
            }
            catch
            {
                
                if (newFilePath != null && System.IO.File.Exists(newFilePath))
                {
                    System.IO.File.Delete(newFilePath);
                }

                throw;
            }

            if (file != null &&
                !string.IsNullOrWhiteSpace(oldStoredFileName))
            {
                string oldFilePath = Path.Combine(
                    _uploadFolder,
                    Path.GetFileName(oldStoredFileName));

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var item = _context.TodoItem
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _context.TodoItem.Find(id);

            if (item == null)
            {
                return NotFound();
            }

            string? storedFileName = item.StoredFileName;

            _context.TodoItem.Remove(item);
            _context.SaveChanges();

            if (!string.IsNullOrWhiteSpace(storedFileName))
            {
                string filePath = Path.Combine(
                    _uploadFolder,
                    Path.GetFileName(storedFileName));

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}