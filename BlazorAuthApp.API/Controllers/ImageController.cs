using BlazorAuthApp.API.Data;
using BlazorAuthApp.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlazorAuthApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
      

        public ImageController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
            
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] ImageData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Base64Image))
            {
                return BadRequest("Dati non validi");
            }
            try
            {
                string base64Image = data.Base64Image;
                string cleanBase64 = base64Image.Replace("data:image/png;base64,", "");
                byte[] imageBytes = Convert.FromBase64String(cleanBase64);
                var imageEntity = new ImageEntity
                {
                    ImageData = imageBytes,
                    FileName = data.FileName ?? Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _applicationDbContext.Add(imageEntity);
                await _applicationDbContext.SaveChangesAsync();
                return Ok("Immagine salvata correttamente");
            }
            catch (FormatException ex)
            {
                // Errore nella decodifica base64. 
                return StatusCode(500, $"Errore di formato durante la decodifica dell'immagine: {ex.Message}");
            }
            catch (Exception ex)
            {
                //  Gestisci altri tipi di eccezioni.
                return StatusCode(500, $"Errore durante il salvataggio dell'immagine: {ex.Message}");
            }
        }
    }
    public class ImageData
    {
        public string Base64Image { get; set; }
        public string? FileName { get; set; } // Opzionale: nome del file
    }
}
