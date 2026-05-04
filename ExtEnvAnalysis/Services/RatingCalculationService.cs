using ExtEnvAnalysis.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtEnvAnalysis.Services;

public static class RatingCalculationService
{
    public static double CalculateWeightedRating(IEnumerable<RatingRow>? rows, Func<RatingRow, int> scoreSelector)
    {
        if (rows is null) return 0.0;

        double result = rows
            .Where(row => row.IsActive)
            .Sum(row => row.Factor.WeightValue * scoreSelector(row));

        return Math.Round(result, 2, MidpointRounding.AwayFromZero);
    }

    public static (double My, double A, double B, double C) CalculateCompanyTotals(IEnumerable<RatingRow>? rows)
    {
        return (
            CalculateWeightedRating(rows, row => row.MyValue),
            CalculateWeightedRating(rows, row => row.AValue),
            CalculateWeightedRating(rows, row => row.BValue),
            CalculateWeightedRating(rows, row => row.CValue));
    }
}
