using SQLite;

namespace Journal.Models
{
    public class EntryTag
    {
        [PrimaryKey, AutoIncrement]
        public int EntryTagId { get; set; }

        public int EntryId { get; set; }   // FK
        public int TagId { get; set; }     // FK
    }
}
