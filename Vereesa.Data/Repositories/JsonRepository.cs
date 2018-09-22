using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Giveaways;

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

            Console.WriteLine($"Initialized JsonRepository for type {typeof(T).Name} at {_filePath}.");
        }

        private List<FileInfo> GetRepositoryFiles() 
        {
            var directory = new DirectoryInfo(_filePath);
            var files = directory.EnumerateFiles($"{typeof(T).Name}.*.json").ToList();
            return files;
        }

        public IEnumerable<T> GetAll()
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

            return _context;
        }

        public void Add(T entity)
        {
            Console.WriteLine($"Adding new entity of type {typeof(T).Name}.");
            _context.Add(entity);
        }

        public void Save()
        {
            Console.WriteLine($"Saving entities of type {typeof(T).Name}.");

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
                    Console.WriteLine($"Failed save entity: {entity.Id} ({typeof(T).Name}).", ex);
                }
            }

            Console.WriteLine($"Saved {savedEntities} entities of type {typeof(T).Name}.");
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
