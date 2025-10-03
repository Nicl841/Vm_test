using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using DailyProduction.Models;

namespace IbasAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DailyProductionController : ControllerBase
    {
        private readonly List<DailyProductionDTO> _productionRepo;
        private readonly ILogger<DailyProductionController> _logger;

        public DailyProductionController(
            ILogger<DailyProductionController> logger,
            IConfiguration config,
            IWebHostEnvironment env) // giver adgang til projektroden
        {
            _logger = logger;

            // Læs Environment fra config (Local / Azure)
            var environment = config["Environment"] ?? "Local";

            // Find sti fra appsettings.json
            var csvPath = config[$"CsvFilePath:{environment}"];

            // Hvis lokal → byg sti relativt til ContentRootPath (projektroden)
            if (environment == "Local" && !Path.IsPathRooted(csvPath))
            {
                csvPath = Path.Combine(env.ContentRootPath, csvPath);
            }

            _logger.LogInformation($"Using CSV path: {csvPath}");

            _productionRepo = LoadProductionDataFromCsv(csvPath);
        }

        private List<DailyProductionDTO> LoadProductionDataFromCsv(string filePath)
        {
            var productionList = new List<DailyProductionDTO>();

            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError($"CSV file not found: {filePath}");
                    return productionList;
                }

                var lines = System.IO.File.ReadAllLines(filePath);

                // Skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    var columns = lines[i].Split(',');

                    if (columns.Length >= 4 &&
                        int.TryParse(columns[0], out int partitionKey) &&
                        DateTime.TryParse(columns[1], out DateTime date) &&
                        int.TryParse(columns[3], out int itemsProduced))
                    {
                        BikeModel model = partitionKey switch
                        {
                            1 => BikeModel.IBv1,
                            2 => BikeModel.evIB100,
                            3 => BikeModel.evIB200,
                            _ => BikeModel.undefined
                        };

                        productionList.Add(new DailyProductionDTO
                        {
                            Date = date,
                            Model = model,
                            ItemsProduced = itemsProduced
                        });
                    }
                }

                _logger.LogInformation($"Loaded {productionList.Count} production records from CSV");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading production data from CSV");
            }

            return productionList;
        }

        [HttpGet]
        public IEnumerable<DailyProductionDTO> Get()
        {
            return _productionRepo;
        }
    }
}
