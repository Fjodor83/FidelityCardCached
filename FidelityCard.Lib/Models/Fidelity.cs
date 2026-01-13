using FidelityCard.Lib.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FidelityCard.Lib.Models;

public class Fidelity
{
    [Key]
    public int IdFidelity { get; set; }
    [Column(TypeName = "varchar(20)")]
    public string? CdFidelity { get; set; }
    [Required]
    [Column("CdNe", TypeName = "varchar(6)")]
    [StringLength(6)]
    public string? Store { get; set; }
    [Required(ErrorMessage = "Il campo Cognome è obbligatorio")]
    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    [MinLength(1)]
    [RegularExpression(@"^[a-zA-Z\s']+$", ErrorMessage = "Solo lettere")]
    public string? Cognome { get; set; }
    [Required(ErrorMessage = "Il campo Nome è obbligatorio")]
    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    [MinLength(1)]
    [RegularExpression(@"^[a-zA-Z\s']+$", ErrorMessage = "Solo lettere")]
    public string? Nome { get; set; }
    [Required(ErrorMessage = "Il campo Data di nascita è obbligatorio")]
    [Column(TypeName = "smalldatetime")]
    [DateRange]
    public DateTime? DataNascita { get; set; }
    [Required(ErrorMessage = "Email obbligatoria")]
    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    [Required(ErrorMessage = "Il campo Sesso è obbligatorio")]
    [Column(TypeName = "char(1)")]
    [StringLength(1)]
    public string? Sesso { get; set; }
    [Required(ErrorMessage = "Il campo Indirizzo è obbligatorio")]
    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    [MinLength(1)]
    public string? Indirizzo { get; set; }
    [Required(ErrorMessage = "Il campo Località è obbligatorio")]
    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    [MinLength(1)]
    public string? Localita { get; set; }
    [Required(ErrorMessage = "Il campo CAP è obbligatorio")]
    [Column(TypeName = "varchar(10)")]
    [StringLength(10)]
    public string? Cap { get; set; }
    [Required(ErrorMessage = "Il campo Provincia è obbligatorio")]
    [Column(TypeName = "char(2)")]
    [StringLength(2)]
    [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Solo lettere")]
    public string? Provincia { get; set; }
    [Required(ErrorMessage = "Il campo Nazione è obbligatorio")]
    [Column(TypeName = "char(2)")]
    [StringLength(2)]
    [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Solo lettere")]
    public string? Nazione { get; set; }
    [Required(ErrorMessage = "Il campo Cellulare è obbligatorio")]
    [Column(TypeName = "varchar(20)")]
    [StringLength(20)]
    [Phone]
    [RegularExpression(@"^\+?[0-9]+$", ErrorMessage = "Solo numeri")]
    public string? Cellulare { get; set; }
}
