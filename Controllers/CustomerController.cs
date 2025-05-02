using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomersAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerController : ControllerBase
    {
        private const string StorageFile = "customers.json";
        private static List<Customer> _customers = new();

        static CustomerController()
        {
            _customers = LoadCustomersFromFile();
        }

        private static List<Customer> LoadCustomersFromFile()
        {
            try
            {
                if (System.IO.File.Exists(StorageFile))
                {
                    var json = System.IO.File.ReadAllText(StorageFile);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // Handle casing differences
                    return JsonSerializer.Deserialize<List<Customer>>(json, options) ?? new List<Customer>();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading customers from file: {ex.Message}");
            }
            return new List<Customer>(); // Return empty list if file doesn't exist or deserialization fails
        }

        [HttpPost]
        public IActionResult Post([FromBody] List<Customer> newCustomers)
        {
            if (newCustomers == null || newCustomers.Count == 0)
            {
                return BadRequest("No customers provided in the request body.");
            }

            try
            {
                var ids = _customers.Select(c => c.Id).ToHashSet();
                var validationErrors = new List<string>();

                foreach (var cust in newCustomers)
                {
                    // Validation
                    if (!IsValidCustomer(cust, ids, validationErrors))
                    {
                        continue; // Skip to the next customer if validation fails
                    }

                    // Insertion (Sorted)
                    InsertCustomerSorted(cust);
                    ids.Add(cust.Id);
                }

                if (validationErrors.Any())
                {
                    return BadRequest(string.Join("\n", validationErrors)); // Return all validation errors
                }

                SaveCustomersToFile(); // Save after all customers are processed

                return Ok();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing customers: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        private bool IsValidCustomer(Customer cust, HashSet<int> existingIds, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(cust.FirstName) || string.IsNullOrWhiteSpace(cust.LastName))
            {
                errors.Add($"Customer ID {cust.Id}: First name and last name cannot be empty.");
                return false;
            }

            if (cust.Age <= 0)
            {
                errors.Add($"Customer ID {cust.Id}: Age must be a positive number.");
                return false;
            }

            if (cust.Age <= 18)
            {
                errors.Add($"Customer ID {cust.Id}: Age must be over 18.");
                return false;
            }

            if (cust.Id <= 0)
            {
                errors.Add($"Customer ID {cust.Id}: ID must be a positive number.");
                return false;
            }

            if (existingIds.Contains(cust.Id))
            {
                errors.Add($"Customer ID {cust.Id} already exists.");
                return false;
            }

            return true;
        }

        private void InsertCustomerSorted(Customer cust)
        {
            int index = 0;
            while (index < _customers.Count)
            {
                var existing = _customers[index];
                int compare = string.Compare(cust.LastName, existing.LastName, StringComparison.OrdinalIgnoreCase); // Case-insensitive comparison
                if (compare < 0 || (compare == 0 && string.Compare(cust.FirstName, existing.FirstName, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    break;
                }
                index++;
            }
            _customers.Insert(index, cust);
        }

        private void SaveCustomersToFile()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true }; // For readability
                var json = JsonSerializer.Serialize(_customers, options);
                System.IO.File.WriteAllText(StorageFile, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving customers to file: {ex.Message}");
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<Customer>> Get()
        {
            try
            {
                return Ok(_customers);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting customers: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving customers.");
            }
        }
    }

    public class Customer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }
    }
}