using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Task.Models
{
    public class TodoItem
    {
        public int Id { get; set; }


        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public String Title { get; set; } = string.Empty;


        [StringLength(4000)]
        public String? Description { get; set; }

        [Required(ErrorMessage = "DeadLine is required")]
        public DateTime? DeadLine { get; set; }

        public string? FileName { get; set; } // This property will store the original file name of the uploaded file

        public string? StoredFileName { get; set; } // This property will store the unique file name used for storage


    }
}