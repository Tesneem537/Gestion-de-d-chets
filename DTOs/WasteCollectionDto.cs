using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Globalization;
public class WasteCollectionDto
{
    [Required]
    public string CollectorName { get; set; }

    [Required]
    public string HotelName { get; set; }

    [Required]
    public string TruckName { get; set; }

    [Required]
    public string WasteTypeName { get; set; }

    [Required]
    public double Quantity { get; set; }

    public string Comment { get; set; }

    // Photo
    [MaxFileSize(5 * 1024 * 1024)]
    [AllowedExtensions(new string[] { ".jpg", ".jpeg", ".png" })]
    public IFormFile? Photo { get; set; }
    public DateTime EntryTime { get;  set; }
}


public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly int _maxSize;
    public MaxFileSizeAttribute(int maxSize)
    {
        _maxSize = maxSize;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var file = value as IFormFile;
        if (file != null && file.Length > _maxSize)
        {
            return new ValidationResult($"File size should not exceed {_maxSize / (1024 * 1024)} MB.");
        }
        return ValidationResult.Success;
    }
}

// Custom Validation Attribute for allowed file extensions
public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;
    public AllowedExtensionsAttribute(string[] extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var file = value as IFormFile;
        if (file != null)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_extensions.Contains(extension))
            {
                return new ValidationResult($"Invalid file extension. Only the following extensions are allowed: {string.Join(", ", _extensions)}");
            }
        }
        return ValidationResult.Success;
    }
}