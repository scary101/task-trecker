using Renci.SshNet;

namespace steptreck.API.Services.Backup
{
    public class BackupService
    {
        private readonly string _sshHost;
        private readonly string _sshUser;
        private readonly string _sshPassword;

        private readonly string _dbName;
        private readonly string _dbUser;
        private readonly string _dbPassword;

        public BackupService()
        {
            _sshHost = "62.109.2.222";
            _sshUser = "root";
            _sshPassword = "Cognition2647465";

            _dbName = "steptreck_db";
            _dbUser = "super";
            _dbPassword = "super";
        }

        public async Task<byte[]> CreateBackupAsync()
        {
            string remotePath = "/tmp/backup.sql";
            using (var ssh = new SshClient(_sshHost, _sshUser, _sshPassword))
            {
                ssh.Connect();
                string cmd = $"export PGPASSWORD='{_dbPassword}'; pg_dump -h 127.0.0.1 -U {_dbUser} {_dbName} > {remotePath}";

                var command = ssh.CreateCommand(cmd);
                command.Execute();

                if (!string.IsNullOrEmpty(command.Error))
                {
                    ssh.Disconnect();
                    throw new Exception($"pg_dump failed: {command.Error}");
                }

                ssh.Disconnect();
            }

            using (var sftp = new SftpClient(_sshHost, _sshUser, _sshPassword))
            {
                sftp.Connect();
                using var ms = new MemoryStream();
                sftp.DownloadFile(remotePath, ms);
                ms.Position = 0;
                sftp.DeleteFile(remotePath);
                sftp.Disconnect();
                return ms.ToArray();
            }
        }

        public async Task RestoreBackupAsync(IFormFile file)
        {
            string remotePath = "/tmp/restore.sql";

            using (var sftp = new SftpClient(_sshHost, _sshUser, _sshPassword))
            {
                sftp.Connect();

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                sftp.UploadFile(ms, remotePath);
                sftp.Disconnect();
            }

            using (var ssh = new SshClient(_sshHost, _sshUser, _sshPassword))
            {
                ssh.Connect();

                string cmd =
                    $"export PGPASSWORD='{_dbPassword}'; " +
                    $"psql -U {_dbUser} -d {_dbName} < {remotePath}";

                ssh.CreateCommand(cmd).Execute();
                ssh.CreateCommand($"rm {remotePath}").Execute();

                ssh.Disconnect();
            }
        }
    }
}
