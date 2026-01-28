using SQLite;
using System.ComponentModel.DataAnnotations;

namespace Journal.Models
{
    public class Tag
    {
        [PrimaryKey, AutoIncrement]
        public int TagId { get; set; }
        [Required(ErrorMessage = "Tag is required")]
        public string Tagname { get; set; } = string.Empty;
        public Boolean IsPredefined { get; set; } = false;
    }
}
