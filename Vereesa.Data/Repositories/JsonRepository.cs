using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Giveaways;

namespace Vereesa.Data.Repositories
{
    public class JsonRepository<T> : IRepository<T> where T : IEntity
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

        private List<FileInfo> GetRepositoryFiles()
        {
            var directory = new DirectoryInfo(_filePath);
            var files = directory.EnumerateFiles($"{typeof(T).Name}.*.json").ToList();
            return files;
        }

        public IEnumerable<T> GetAll()
        {
            try
            {
                var files = GetRepositoryFiles();

                foreach (var file in files)
                {
                    var fileId = file.Name.Split('.')[1];

                    if (!_context.Any(e => e.Id == fileId))
                    {
                        _context.Add(JsonConvert.DeserializeObject<T>(File.ReadAllText($"{_filePath}/{file.Name}")));
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return _context;
        }

        public T Add(T entity)
        {
            _context.Add(entity);
            return entity;
        }

        public void Save()
        {
            try
            {
                var savedEntities = 0;

                foreach (var entity in _context)
                {
                    try
                    {
                        File.WriteAllText($"{_filePath}/{typeof(T).Name}.{entity.Id}.json", JsonConvert.SerializeObject(entity));
                        savedEntities++;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void AddOrEdit(T entity)
        {
            if (_context.Any(e => e.Id == entity.Id))
            {
                //do nothing? or replace? its probably the same object, but who knows?
            }
            else
            {
                Add(entity);
            }
        }
    }
}
