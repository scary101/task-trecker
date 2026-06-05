using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace steptreck.Domain.DTOs
{
    public record Point(long t, double v);
    public record Series(string name, List<Point> points);
    public sealed class OpsDashboardDto
    {
        public List<Series> rps { get; set; } = new();
        public List<Series> p95 { get; set; } = new();
        public List<Series> err5 { get; set; } = new();
        public List<Series> cpu { get; set; } = new();
        public List<Series> ram { get; set; } = new();
        public List<Series> active { get; set; } = new();
    }
}
