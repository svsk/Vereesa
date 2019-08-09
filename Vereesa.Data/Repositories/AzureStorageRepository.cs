using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Vereesa.Data.Configuration;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Repositories
{
    public class AzureStorageRepository<T> : IRepository<T> where T : IEntity
    {
        private CloudBlobClient _client;
        private CloudBlobContainer _container;

        public AzureStorageRepository(AzureStorageSettings settings) 
        {
            CloudStorageAccount.TryParse(settings.ConnectionString, out CloudStorageAccount storageAccount);
            _client = storageAccount.CreateCloudBlobClient();
            _container = _client.GetContainerReference(typeof(T).Name.ToLowerInvariant());
            _container.CreateIfNotExists();
        }

        public T Add(T entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Id)) 
            {
                entity.Id = Guid.NewGuid().ToString();
            }
            
            CloudBlockBlob blob = _container.GetBlockBlobReference(entity.Id);
            string blobContent = JsonConvert.SerializeObject(entity);
            blob.UploadText(blobContent, Encoding.UTF8);

            return entity;
        }

        public void AddOrEdit(T entity)
        {
            Add(entity);
        }

        public void Delete(T entity)
        {
            CloudBlockBlob blob = _container.GetBlockBlobReference(entity.Id);
            blob.DeleteIfExists();
        }

        public T FindById(string id)
        {
            T result = default(T);

            try 
            {
                CloudBlockBlob blob = _container.GetBlockBlobReference(id);
                string blobContent = blob.DownloadText();
                result = JsonConvert.DeserializeObject<T>(blobContent);
            }
            catch {}
            
            return result;
        }

        public IEnumerable<T> GetAll()
        {
            List<T> result = new List<T>();
            List<IListBlobItem> listBlobs = _container.ListBlobs().ToList();
            
            foreach (var listBlob in listBlobs) 
            {
                string blobId = ((CloudBlockBlob)listBlob).Name;
                result.Add(FindById(blobId));
            }

            return result;
        }

        public void Save()
        {
        }
    }
}