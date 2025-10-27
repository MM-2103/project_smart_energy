using Microsoft.JSInterop;

namespace SmartEnergy.Client.Services;

public class ChartService
{
    private readonly IJSRuntime _jsRuntime;
    
    public ChartService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task<IJSObjectReference> CreateLineChartAsync(string canvasId, LineChartData chartData)
    {
        return await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "chartFunctions.createLineChart", canvasId, chartData);
    }
    
    public async Task<IJSObjectReference> CreateBarChartAsync(string canvasId, BarChartData chartData)
    {
        return await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "chartFunctions.createBarChart", canvasId, chartData);
    }
    
    public async Task DestroyChartAsync(IJSObjectReference chart)
    {
        if (chart != null)
        {
            await _jsRuntime.InvokeVoidAsync("chartFunctions.destroyChart", chart);
            await chart.DisposeAsync();
        }
    }
}

public class LineChartData
{
    public string Title { get; set; } = "";
    public string XAxisLabel { get; set; } = "";
    public string YAxisLabel { get; set; } = "";
    public List<string> Labels { get; set; } = new();
    public string Dataset1Label { get; set; } = "";
    public List<double> Dataset1Data { get; set; } = new();
    public string Dataset2Label { get; set; } = "";
    public List<double> Dataset2Data { get; set; } = new();
}

public class BarChartData
{
    public string Title { get; set; } = "";
    public string XAxisLabel { get; set; } = "";
    public string YAxisLabel { get; set; } = "";
    public List<string> Labels { get; set; } = new();
    public string DatasetLabel { get; set; } = "";
    public List<double> Data { get; set; } = new();
}