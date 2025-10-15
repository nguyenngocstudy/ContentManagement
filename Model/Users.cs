using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Backend.Model
{
    [Table("users", Schema = "auth")]
    public class Users
    {
        [Key]
        public string user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public byte[]? password_hash { get; set; }
        public DateTime? created_at { get; set; }
        public string role { get; set; }
    }
}
