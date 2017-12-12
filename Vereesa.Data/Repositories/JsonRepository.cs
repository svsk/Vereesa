using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Repositories
{
    public class JsonRepository<T> where T : IEntity
    {
        private List<T> _context;
        private string _filePath;

        public JsonRepository()
        {
            _filePath = $@"{AppContext.BaseDirectory}/data";
            if (Directory.Exists(_filePath) == false)
            {
                Directory.CreateDirectory(_filePath);
            }

            _context = new List<T>();
        }

        public IEnumerable<T> GetAll()
        {
            var directory = new DirectoryInfo(_filePath);
            var files = directory.EnumerateFiles($"{typeof(T).Name}.*.json");

            foreach (var file in files)
            {
                var fileId = file.Name.Split('.')[1];

                if (!_context.Any(e => e.Id == fileId))
                {
                    _context.Add(JsonConvert.DeserializeObject<T>(File.ReadAllText($"{_filePath}/{file.Name}")));
                }
            }

            return _context;
        }

        public void Add(T entity)
        {
            _context.Add(entity);
        }

        public void Save()
        {
            foreach (var entity in _context)
            {
                File.WriteAllText($"{_filePath}/{typeof(T).Name}.{entity.Id}.json", JsonConvert.SerializeObject(entity));
            }
        }
    }
}
