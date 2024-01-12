using System.Collections.Generic;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Data.Models.Commands
{
    public class Command : IEntity
    {
        public string Id { get; set; }
        public IList<string> TriggerCommands { get; set; }
        public CommandTypeEnum CommandType { get; set; }
        public string ReturnMessage { get; set; }
    }
}
