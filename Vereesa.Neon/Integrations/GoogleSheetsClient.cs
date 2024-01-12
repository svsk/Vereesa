using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Vereesa.Neon.Integrations.Interfaces;

namespace Vereesa.Neon.Integrations
{
    public class GoogleSheetsClient : ISpreadsheetClient
    {
        private string[] _scopes = { SheetsService.Scope.Spreadsheets };
        private string _applicationName = "Vereesa";
        private string _sheetId;
        private SheetsService _googleSheetService;

        private SheetsService CreateGoogleSheetsService()
        {
            GoogleCredential credential;
            using (
                Stream stream = new FileStream(
                    Path.Join(AppContext.BaseDirectory, @"googleservicekey.json"),
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                )
            )
            {
                credential = GoogleCredential.FromStream(stream);
            }

            credential = credential.CreateScoped(new[] { SheetsService.Scope.Spreadsheets });

            string bearer;
            try
            {
                Task<string> task = ((ITokenAccess)credential).GetAccessTokenForRequestAsync();
                task.Wait();
                bearer = task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }

            // Create Google Sheets API service.
            return new SheetsService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName
                }
            );
        }

        public IList<IList<object>> GetValueRange(string rangeAddress)
        {
            EnsureSheetOpened();

            var request = _googleSheetService.Spreadsheets.Values.Get(_sheetId, rangeAddress);

            var response = request.Execute();

            return response.Values;
        }

        private void EnsureSheetOpened()
        {
            if (string.IsNullOrWhiteSpace(_sheetId))
            {
                throw new InvalidOperationException(
                    $@"Please use the {nameof(Open)}() method to open a sheet before operating on it."
                );
            }
        }

        public void Open(string sheetIdentifier)
        {
            _googleSheetService = _googleSheetService ?? CreateGoogleSheetsService();
            _sheetId = sheetIdentifier;
        }

        public void SetCellValue(string cellAddress, object value)
        {
            throw new NotImplementedException();
        }
    }
}
