namespace BlobBackup.Test;

public record Box(BlobProvider BlobProvider, BackupProvider BackupProvider, RestoreProvider RestoreProvider, FileProvider FileProvider);
