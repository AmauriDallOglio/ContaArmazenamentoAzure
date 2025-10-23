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
 
            _connectionString = configuration.GetValue<string>("AzureContaArmazenamento");
            _containerName = configuration.GetValue<string>("AzureContaArmazenamentoContainerNome");
 
        }

        [HttpPost("Carregar")]
        public async Task<IActionResult> CarregarArquivo(IFormFile arquivo)
        {
            //-------------------------------------------------------------
            //Validar anexo
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Nenhum arquivo enviado.");

            // Lista de extensões permitidas
            var extensoesPermitidas = new HashSet<string>
            {
                ".jpg", ".jpeg", ".png", ".gif",
                ".mp4", ".avi", ".mov",
                ".pdf",
                ".zip", ".rar"
            };


            // Obtém a extensão do arquivo
            string extensao = Path.GetExtension(arquivo.FileName)?.ToLower();
            // Valida se é uma extensão permitida
            if (string.IsNullOrEmpty(extensao) || !extensoesPermitidas.Contains(extensao))
                return BadRequest("Tipo de arquivo não permitido.");


            //-------------------------------------------------------------
            //Carrega o conteiner
            BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);

            ////-------------------------------------------------------------
            ////Validar se existe aktian/Amauri/ImagemAmauri na conta de armazenamento
            //var resultado = new List<string>();
            //await foreach (BlobHierarchyItem item in containerClient.GetBlobsByHierarchyAsync(delimiter: "/"))
            //{
            //    if (item.IsPrefix)
            //    {
            //        Console.WriteLine("Pasta: " + item.Prefix);
            //        resultado.Add($"[Pasta] {item.Prefix}");
            //    }
            //    else
            //    {
            //        resultado.Add($"[Arquivo] {item.Blob.Name}");
            //    }
            //}
            //await containerClient.CreateIfNotExistsAsync();

            ////-------------------------------------------------------------
            //// Verifica se já existe algum blob com esse prefixo
            // string caminhoPasta = $"{_sessaoRequisicaoHTTP.TenantId}/Imagem";
            //bool pastaExiste = false;
            //await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: caminhoPasta))
            //{
            //    pastaExiste = true;
            //}

            // Cria um blob com o caminho completo (prefixo + nome do arquivo)



            // Define a subpasta com base na extensão
            string subpasta = extensao switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" => "Imagem",
                ".mp4" or ".avi" or ".mov" => "Videos",
                ".pdf" => "Documentos",
                ".zip" or ".rar" => "ArquivosCompactados",
                _ => "Outros"
            };

            string caminhoPasta = $"Amauri/{subpasta}";
            string nomeBlob = $"{caminhoPasta}/{arquivo.FileName}";

            //-------------------------------------------------------------
            //Adicioma o anexo do destino na pasta criada
            BlobClient blobClient = containerClient.GetBlobClient(nomeBlob);
            using var data = arquivo.OpenReadStream();
            blobClient.Upload(data, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = arquivo.ContentType }
            });
            var propriedades = await blobClient.GetPropertiesAsync();

            return Ok(new
            {
                PastaDestino = caminhoPasta,
                TipoArquivo = arquivo.ContentType,
                NomeBlob = blobClient.Name,
                Uri = blobClient.Uri.ToString(),
                ETag = propriedades.Value.ETag.ToString(),
                LastModified = propriedades.Value.LastModified.ToString("o")
            });
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