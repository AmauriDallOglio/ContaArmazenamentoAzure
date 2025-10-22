using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContaArmazenamentoAzure.Api.Controllers
{
 
    [ApiVersion("1.0")]
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ArquivosController : ControllerBase
    {

        private readonly string _connectionString;
        private readonly string _containerName;

        public ArquivosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetValue<string>("AzureBlobStorage");
            _containerName = configuration.GetValue<string>("AzureBlobContainerName");
        }

        [HttpPost("Carregar")]
        public IActionResult CarregarArquivo(IFormFile arquivo)
        {
            BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);
            BlobClient blobClient = containerClient.GetBlobClient(arquivo.FileName);
            using var data = arquivo.OpenReadStream();
            blobClient.Upload(data, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = arquivo.ContentType }
            });
            return Ok(blobClient.Uri.ToString());
        }


        [HttpGet("Download/{nome}")]
        public IActionResult DownloadArquivo(string nome)
        {
            BlobContainerClient container = new(_connectionString, _containerName);
            BlobClient blob = container.GetBlobClient(nome);
            if (!blob.Exists())
            {
                return BadRequest();
            }

            var retorno = blob.DownloadContent();
            return File(retorno.Value.Content.ToArray(), retorno.Value.Details.ContentType, blob.Name);
        }

        [HttpDelete("Apagar/{nome}")]
        public IActionResult DeletarArquivo(string nome)
        {
            BlobContainerClient containerClient = new(_connectionString, _containerName);
            BlobClient blobClient = containerClient.GetBlobClient(nome);
            blobClient.DeleteIfExists();
            return NoContent();
        }

        [HttpGet("Listar")]
        public IActionResult Listar()
        {
            List<BlobDto> blobDto = new List<BlobDto>();
            BlobContainerClient containerClient = new(_connectionString, _containerName);
            foreach (var blob in containerClient.GetBlobs())
            {
                blobDto.Add(new BlobDto
                {
                    Nome = blob.Name,
                    Tipo = blob.Properties.ContentType,
                    Uri = containerClient.GetBlobClient(blob.Name).Uri.ToString()
                });
            }
            return Ok(blobDto);
        }

        public class BlobDto
        {
            public string Nome { get; set; } = string.Empty;
            public string Tipo { get; set; } = string.Empty;
            public string Uri { get; set; } = string.Empty;
        }

    }

}