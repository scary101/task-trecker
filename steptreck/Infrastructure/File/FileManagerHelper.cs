using Minio;
using Minio.DataModel.Args;

namespace steptreck.API.Infrastructure.File
{
    public class FileManagerHelper
    {
        private readonly IMinioClient _minio;
        private readonly string _bucket;

        public FileManagerHelper(IMinioClient minio, IConfiguration config)
        {
            _minio = minio;
            _bucket = config["Minio:Bucket"]
                      ?? throw new InvalidOperationException("Не задан Minio:Bucket в appsettings.json");
        }
        public async Task EnsureBucketExistsAsync(CancellationToken ct = default)
        {
            try
            {
                var exists = await _minio.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_bucket),
                    ct
                );

                if (!exists)
                {
                    await _minio.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(_bucket),
                        ct
                    );
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось проверить/создать bucket '{_bucket}'. {ex.Message}");
            }
        }
        public async Task UploadAsync(
            string objectKey,
            Stream content,
            string contentType,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Ключ объекта (objectKey) не задан.");

            if (content is null)
                throw new ArgumentNullException(nameof(content), "Содержимое файла не передано.");

            if (!content.CanRead)
                throw new InvalidOperationException("Поток файла недоступен для чтения.");

            if (content.CanSeek)
                content.Position = 0;

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "application/octet-stream";

            try
            {
                await EnsureBucketExistsAsync(ct);

                long size;
                if (content.CanSeek)
                {
                    size = content.Length;
                }
                else
                {
                    using var ms = new MemoryStream();
                    await content.CopyToAsync(ms, ct);
                    ms.Position = 0;
                    content = ms;
                    size = ms.Length;
                }

                var putArgs = new PutObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectKey)
                    .WithStreamData(content)
                    .WithObjectSize(size)
                    .WithContentType(contentType);

                await _minio.PutObjectAsync(putArgs, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось загрузить файл в хранилище. {ex.Message}");
            }
        }
        public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Ключ объекта (objectKey) не задан.");

            try
            {
                var ms = new MemoryStream();

                var getArgs = new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(ms);
                    });

                await _minio.GetObjectAsync(getArgs, ct);

                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось скачать файл из хранилища. {ex.Message}");
            }
        }

        public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Ключ объекта (objectKey) не задан.");

            try
            {
                var removeArgs = new RemoveObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectKey);

                await _minio.RemoveObjectAsync(removeArgs, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось удалить файл из хранилища. {ex.Message}");
            }
        }

        public async Task<string> GetPresignedDownloadUrlAsync(
            string objectKey,
            int expirySeconds = 60,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Ключ объекта (objectKey) не задан.");

            if (expirySeconds < 10 || expirySeconds > 60 * 60)
                throw new ArgumentException("Время жизни ссылки должно быть от 10 секунд до 3600 секунд.");

            try
            {
                var args = new PresignedGetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(objectKey)
                    .WithExpiry(expirySeconds);

                var url = await _minio.PresignedGetObjectAsync(args);
                return url;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось создать ссылку на скачивание. {ex.Message}");
            }
        }
    }
}

