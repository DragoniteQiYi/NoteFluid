using System.IO;

namespace NoteFluid.Core.Services
{
    public class FileService
    {
        private readonly string _midiFolderPath;
        private FileInfo _selectedFile;

        public FileInfo SelectedFile
        {
            get => _selectedFile;
            set => _selectedFile = value;
        }

        public FileService()
        {
            _midiFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NoteFluid",
                "MIDI");
        }

        /// <summary>
        /// 获取所有 .mid 文件的完整路径
        /// </summary>
        public List<string> GetAllMidiFilePaths()
        {
            try
            {
                if (!Directory.Exists(_midiFolderPath))
                {
                    return [];
                }

                return [.. Directory.GetFiles(_midiFolderPath, "*.mid", SearchOption.TopDirectoryOnly)];
            }
            catch (Exception ex)
            {
                // 记录日志或处理异常
                Console.WriteLine($"读取MIDI文件时发生错误: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// 获取所有 .mid 文件的 FileInfo 对象
        /// </summary>
        public List<FileInfo> GetAllMidiFiles()
        {
            try
            {
                if (!Directory.Exists(_midiFolderPath))
                {
                    return [];
                }

                var directoryInfo = new DirectoryInfo(_midiFolderPath);
                var fileInfos = directoryInfo.GetFiles("*.mid", SearchOption.TopDirectoryOnly)
                    .ToList();

                return [.. directoryInfo.GetFiles("*.mid", SearchOption.TopDirectoryOnly)];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取MIDI文件信息时发生错误: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// 获取所有 .mid 文件的文件名（不含路径）
        /// </summary>
        public List<string> GetAllMidiFileNames()
        {
            try
            {
                if (!Directory.Exists(_midiFolderPath))
                {
                    return [];
                }

                return Directory.GetFiles(_midiFolderPath, "*.mid", SearchOption.TopDirectoryOnly)
                                .Select(Path.GetFileName)
                                .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取MIDI文件名时发生错误: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// 检查指定文件是否存在
        /// </summary>
        public bool MidiFileExists(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var filePath = Path.Combine(_midiFolderPath, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 获取MIDI文件夹路径
        /// </summary>
        public string GetMidiFolderPath()
        {
            return _midiFolderPath;
        }

        /// <summary>
        /// 确保MIDI文件夹存在，如果不存在则创建
        /// </summary>
        public void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_midiFolderPath))
                {
                    Directory.CreateDirectory(_midiFolderPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建MIDI文件夹时发生错误: {ex.Message}");
                throw;
            }
        }

    }
}
