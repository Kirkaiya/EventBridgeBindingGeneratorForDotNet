using System.Collections.Generic;
using System.Linq;
using Amazon.Schemas.Model;
using Amazon.Schemas;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EventBridgeBindingGenerator;
using System.Text;

namespace EventBridgeGeneratorWebUI.Pages
{
    public class IndexModel : PageModel
    {
        public List<SelectListItem> SchemaNames { get; set; } = new List<SelectListItem>();

        public async Task<IActionResult> OnGet()
        {
            await PopulateSchemasList(null);
            return Page();
        }

        public async Task OnPostApplyFilter(string FilterText)
        {
            if (!FilterText.StartsWith("aws."))
                FilterText = "aws." + FilterText;

            await PopulateSchemasList(FilterText);
        }

        public async Task<IActionResult> OnPostGenerateCode(string SchemaSelect)
        {
            var client = new AmazonSchemasClient();
            var request = new DescribeSchemaRequest
            {
                RegistryName = "aws.events",
                SchemaName = SchemaSelect
            };

            var response = await client.DescribeSchemaAsync(request);

            var schema = response.Content;
            var generator = new Generator();
            string schemaName;
            var zipBytes = generator.GenerateCodeFiles(schema, out schemaName);

            return new FileContentResult(zipBytes, "application/zip") { FileDownloadName = schemaName + ".zip" };
        }

        private async Task PopulateSchemasList(string filter)
        {
            var client = new AmazonSchemasClient();
            var request = new ListSchemasRequest { RegistryName = "aws.events", SchemaNamePrefix = filter };

            var response = await client.ListSchemasAsync(request);

            SchemaNames.AddRange(response.Schemas.Select(x => new SelectListItem { Value = x.SchemaName, Text = x.SchemaName }));

        }
    }
}
