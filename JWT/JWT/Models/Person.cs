using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWT.Models;

[Table("Person")]
public class Person
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }

    [Column("Email")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(50)]
    public string Email { get; set; }
        
    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(50)]
    public string Password { get; set; }
}