using DuckDB.NET.Data;
using visualizer.Repositories;

namespace TestProject1;

[Collection("DuckDb seeded")]
public class VisulisationGenerationTest : IClassFixture<DuckDbFixture>
{
    private readonly VisualisationsGenerator generator;
    public VisulisationGenerationTest(DuckDbFixture fixture)
    {
        generator = new VisualisationsGenerator(
            new SQLDecomposer(), 
            new TableGenerator(new SQLExecutor(fixture.CreateConnection())),  
            new TableOriginColumnsGenerator());
    }
    
    [Fact]
    public void Join()
    {
        const string query =
            """
            SELECT * FROM product
            JOIN purchase ON product.productname = purchase.productname
            """;
        TestQuery(query);
    }

    private void TestQuery(string query)
    {
        var visualisations = generator.Generate(query);
        Assert.True(visualisations.Count > 0);
    }
}