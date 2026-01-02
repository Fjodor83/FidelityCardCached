using FidelityCard.Lib.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelityCard.Lib.Models;

public class Fidelity
{
    [Key]
    public int IdFidelity { get; set; }

    [Required]
    [Column(TypeName = "varchar(20)")]
    public string CdFidelity { get; set; } = string.Empty;

    [Required]
    [Column("CdNe", TypeName = "varchar(6)")] 
    public string Store { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    public string Cognome { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varchar(50)")]
    [StringLength(50)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "smalldatetime")]
    [DateRange]
    public DateTime? DataNascita { get; set; } = null;

    [Required]
    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Column(TypeName = "char(1)")]
    [StringLength(1)]
    public string? Sesso { get; set; } = null;

    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    public string? Indirizzo { get; set; } = null;
    
    [Column(TypeName = "varchar(100)")]
    [StringLength(100)]
    public string? Localita { get; set; } = null;

    [Column(TypeName = "varchar(10)")]
    [StringLength(10)]
    public string? Cap { get; set; } = null;

    [Column(TypeName = "char(2)")]
    [StringLength(2)]
    public string? Provincia { get; set; } = null;

    [Column(TypeName = "char(2)")]
    [StringLength(2)]
    public string? Nazione { get; set; } = null;

    [Column(TypeName = "varchar(20)")]
    [StringLength(20)]
    [Phone]
    public string? Cellulare { get; set; } = null;

}
