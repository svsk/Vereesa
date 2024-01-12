using System.Collections.Generic;

namespace Vereesa.Neon.Integrations.Interfaces
{
    public interface ISpreadsheetClient
    {
        /// <summary>
        /// Opens a spreadsheet.
        /// </summary>
        /// <param name="sheetIdentifier">Id or path to the sheet to open.</param>
        void Open(string sheetIdentifier);

        IList<IList<object>> GetValueRange(string rangeAddress);

        void SetCellValue(string cellAddress, object value);
    }
}
