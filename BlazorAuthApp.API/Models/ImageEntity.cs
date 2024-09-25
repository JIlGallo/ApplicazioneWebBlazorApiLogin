using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorAuthApp.API.Models
{
    [Table("ImageEntity")]
    public class ImageEntity
    {
        [Key] 
        [Required]
        public int Id { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
