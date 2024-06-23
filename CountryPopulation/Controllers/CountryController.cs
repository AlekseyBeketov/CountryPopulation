using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
namespace CountryPopulation.Controllers
{
    public class CountryController : Controller
    {
        [HttpPost]
        public IActionResult Index(List<string> selectedCountries, int firstDate, int secondDate)
        {
            TempData["FirstDate"] = firstDate;
            TempData["SecondDate"] = secondDate;
            string countriesJson = JsonConvert.SerializeObject(selectedCountries);
            HttpContext.Session.SetString("SelectedCountries", countriesJson);
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string filePath = "D:\\Downloads\\Численность.xlsx";
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; 
                int rowCount = worksheet.Dimension.Rows-5;
                int colCount = worksheet.Dimension.Columns;

                DataTable dt = new DataTable();
    
                for (int col = 1; col <= colCount; col++)
                {
                    var value = worksheet.Cells[1, col].Value;
                    if (value != null)
                    {
                        dt.Columns.Add(value.ToString());
                    }
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    DataRow dataRow = dt.NewRow();
                    for (int col = 1; col <= colCount; col++)
                    {
                        dataRow[col - 1] = worksheet.Cells[row, col].Value;
                    }
                    dt.Rows.Add(dataRow);
                }
                string countriesJson = HttpContext.Session.GetString("SelectedCountries");
                List<string> selectedCountries = null;

                if (!string.IsNullOrEmpty(countriesJson))
                {
                    selectedCountries = JsonConvert.DeserializeObject<List<string>>(countriesJson);
                }

                if (selectedCountries == null)
                {
                    selectedCountries = new List<string> { "Austria", "Sweden", "Portugal" };
                }

                if (TempData["FirstDate"] == null)
                    ViewBag.firstDate = 1990;
                else
                    ViewBag.firstDate = TempData["FirstDate"];

                if ((TempData["SecondDate"] == null) || (Convert.ToInt32(TempData["SecondDate"]) == 0))
                    ViewBag.secondDate = 2023;
                else
                    ViewBag.secondDate = TempData["SecondDate"];

                ViewBag.Select = selectedCountries;
                return View(dt);
            }
        }

        [HttpPost]
        public IActionResult AddData(int firstDate, int secondDate, string Select, string dataTableJson)
        {
            List<string> selectedCountries = Select.Split(',').ToList();
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(dataTableJson);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string filePath = "D:\\Downloads\\Численность.xlsx";
            string csvFilePath = "D:\\Downloads\\csvFile.csv";

            // Создаем StringBuilder для хранения данных CSV
            StringBuilder csvData = new StringBuilder();
            csvData.Append("Country");
            csvData.Append(",");
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Первый лист в Excel файле
                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Добавляем столбцы, если их нет
                for (int year = firstDate; year <= secondDate; year++)
                {
                    csvData.Append(year);
                    csvData.Append(",");
                }
                csvData.Remove(csvData.Length - 1, 1);
                csvData.AppendLine();

                foreach (string country in selectedCountries)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Country"].ToString() == country)
                        {
                            for (int col = 1; col <= colCount; col++)
                            {
                                var cellValue = worksheet.Cells[row.Table.Rows.IndexOf(row) + 2, col].Value;
                                if (cellValue == null || string.IsNullOrWhiteSpace(cellValue.ToString()))
                                {
                                    // worksheet.Cells[row.Table.Rows.IndexOf(row) + 2, col].Value = row[col - 2];
                                    csvData.Append(row[col - 2]);
                                    csvData.Append(",");
                                }
                                else
                                {
                                    csvData.Append(cellValue);
                                    csvData.Append(",");
                                }                                
                            }
                            csvData.Remove(csvData.Length - 1, 1);
                            csvData.AppendLine();
                        }
                    }
                }
                using (StreamWriter writer = new StreamWriter(csvFilePath))
                {
                    writer.Write(csvData.ToString());
                }

                //package.Save();
            }
            var fileBytes = System.IO.File.ReadAllBytes(csvFilePath);
            var fileName = "Result.csv";
            return File(fileBytes, "application/octet-stream", fileName);
        }

    }
}