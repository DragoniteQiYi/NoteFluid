using NoteFluid.Core.Configs;
using System.IO;
using System.Text.Json;

namespace NoteFluid.Core.Services
{
    public class ConfigService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly string _filePath;
        private readonly ConfigData _configData;

        public ConfigData ConfigData
        {
            get => _configData;
        }

        /// <summary>
        /// 初始化配置服务
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <param name="writeIndented">是否格式化输出</param>
        public ConfigService()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };

            _configData = Read<ConfigData>();
            Console.WriteLine();
        }

        /// <summary>
        /// 读取配置（不存在则返回默认值）
        /// </summary>
        public T Read<T>() where T : class, new()
        {
            try
            {
                _semaphore.Wait();

                if (!File.Exists(_filePath))
                    return new T();

                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            }
            catch (JsonException)
            {
                return new T();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 异步读取配置
        /// </summary>
        public async Task<T> ReadAsync<T>() where T : class, new()
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!File.Exists(_filePath))
                    return new T();

                string json = await File.ReadAllTextAsync(_filePath);
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                return result ?? new T();
            }
            catch (JsonException)
            {
                return new T();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 写入配置（自动创建目录）
        /// </summary>
        public void Write<T>(T config) where T : class
        {
            try
            {
                _semaphore.Wait();
                EnsureDirectoryExists();
                string json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 异步写入配置
        /// </summary>
        public async Task WriteAsync<T>(T config) where T : class
        {
            await _semaphore.WaitAsync();

            try
            {
                EnsureDirectoryExists();
                string json = JsonSerializer.Serialize(config, _jsonOptions);
                await File.WriteAllTextAsync(_filePath, json);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 更新部分配置（合并写入）
        /// </summary>
        public void Update<T>(Action<T> updateAction) where T : class, new()
        {
            try
            {
                _semaphore.Wait();

                T config = Read<T>();
                updateAction(config);
                Write(config);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 异步更新部分配置
        /// </summary>
        public async Task UpdateAsync<T>(Action<T> updateAction) where T : class, new()
        {
            await _semaphore.WaitAsync();

            try
            {
                T config = await ReadAsync<T>();
                updateAction(config);
                await WriteAsync(config);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 检查配置文件是否存在
        /// </summary>
        public bool Exists()
        {
            return File.Exists(_filePath);
        }

        /// <summary>
        /// 删除配置文件
        /// </summary>
        public void Delete()
        {
            try
            {
                _semaphore.Wait();
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Save()
        {
            Write(_configData);
        }

        private void EnsureDirectoryExists()
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
