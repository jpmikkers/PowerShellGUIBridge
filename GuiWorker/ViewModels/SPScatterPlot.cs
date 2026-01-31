using System.Collections.Generic;

namespace GuiWorker.ViewModels;

public class SPScatterPlot
{
    public string? Title { get; set; }
    public string? XAxisLabel { get; set; }
    public string? YAxisLabel { get; set; }
    public List<double>? XValues { get; set; }
    public List<double>? YValues { get; set; }
    public string? LegendText { get; set; }
    public string? Color { get; set; }
    public double MarkerSize { get; set; } = 5;
}