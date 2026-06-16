using System;
using System.Collections.Generic;
using UnityEngine;

public class GestureTemplate
{
    public string Name;
    public List<Vector2> Points;

    public GestureTemplate(string name, List<Vector2> points)
    {
        this.Name = name;
        this.Points = GestureRecognizerML.Resample(points, GestureRecognizerML.SamplingPoints);
        GestureRecognizerML.ScaleTo(this.Points, GestureRecognizerML.SquareSize);
        GestureRecognizerML.TranslateTo(this.Points, Vector2.zero);
    }
}

public static class GestureRecognizerML
{
    public const int SamplingPoints = 32;
    public const float SquareSize = 250f;
    private static List<GestureTemplate> trainingSet = new List<GestureTemplate>();

    static GestureRecognizerML()
    {
        LoadDefaultTrainingTemplates();
    }

    public static string Classify(List<Vector2> inputPoints, out float confidenceScore)
    {
        confidenceScore = 0f;
        if (inputPoints.Count < 5) return "Unknown";

        // Pre-process the live user drawing using identical training-set filters
        List<Vector2> processedInput = Resample(inputPoints, SamplingPoints);
        ScaleTo(processedInput, SquareSize);
        TranslateTo(processedInput, Vector2.zero);

        string bestMatch = "Unknown";
        float minDistance = float.MaxValue;

        // Nearest-Neighbor ML Match Loop
        foreach (var template in trainingSet)
        {
            float dist = GreedyCloudMatch(processedInput, template.Points);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestMatch = template.Name;
            }
        }

        // Convert the structural distance error into a 0.0 - 1.0 confidence percentage score
        confidenceScore = Mathf.Max(0f, 1f - (minDistance / (0.5f * Mathf.Sqrt(SquareSize * SquareSize * 2))));
        return bestMatch;
    }

    private static float GreedyCloudMatch(List<Vector2> points1, List<Vector2> points2)
    {
        float e = 0.5f;
        int step = Mathf.FloorToInt(Mathf.Pow(points1.Count, 1.0f - e));
        if (step < 1) step = 1;

        float minSum = float.MaxValue;
        for (int i = 0; i < points1.Count; i += step)
        {
            float d1 = CloudDistance(points1, points2, i);
            float d2 = CloudDistance(points2, points1, i);
            minSum = Mathf.Min(minSum, Mathf.Min(d1, d2));
        }
        return minSum;
    }

    private static float CloudDistance(List<Vector2> pts1, List<Vector2> pts2, int startIdx)
    {
        bool[] matched = new bool[pts2.Count];
        float sum = 0;
        int i = startIdx;

        do
        {
            int index = -1;
            float minDist = float.MaxValue;
            for (int j = 0; j < pts2.Count; j++)
            {
                if (!matched[j])
                {
                    float d = Vector2.Distance(pts1[i], pts2[j]);
                    if (d < minDist)
                    {
                        minDist = d;
                        index = j;
                    }
                }
            }
            if (index != -1) matched[index] = true;
            sum += (pts1.Count - i) * minDist;
            i = (i + 1) % pts1.Count;
        } while (i != startIdx);

        return sum / (pts1.Count * pts1.Count);
    }

    public static List<Vector2> Resample(List<Vector2> points, int n)
    {
        List<Vector2> resampled = new List<Vector2> { points[0] };
        float pathLength = 0f;
        for (int i = 1; i < points.Count; i++) pathLength += Vector2.Distance(points[i - 1], points[i]);

        float interval = pathLength / (n - 1);
        float accumulatedDist = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            float d = Vector2.Distance(points[i - 1], points[i]);
            if (accumulatedDist + d >= interval)
            {
                float t = (interval - accumulatedDist) / d;
                Vector2 q = Vector2.Lerp(points[i - 1], points[i], t);
                resampled.Add(q);
                points.Insert(i, q);
                accumulatedDist = 0f;
            }
            else accumulatedDist += d;
        }
        while (resampled.Count < n) resampled.Add(points[points.Count - 1]);
        return resampled;
    }

    public static void ScaleTo(List<Vector2> points, float size)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 p in points)
        {
            if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
        }
        float w = maxX - minX;
        float h = maxY - minY;
        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Vector2(points[i].x * (size / (w > 0 ? w : 1f)), points[i].y * (size / (h > 0 ? h : 1f)));
        }
    }

    public static void TranslateTo(List<Vector2> points, Vector2 target)
    {
        Vector2 centroid = Vector2.zero;
        foreach (Vector2 p in points) centroid += p;
        centroid /= points.Count;
        for (int i = 0; i < points.Count; i++) points[i] += (target - centroid);
    }

    private static void LoadDefaultTrainingTemplates()
    {
        // =================================================================
        //            PRODUCTION CALIBRATED MULTI-STROKE GESTURES
        // =================================================================

        // --- 1. FIRE (Double Nested Triangles) ---
        // Expects outer triangle path directly proceeding into inner triangle path points
        trainingSet.Add(new GestureTemplate("Fire", new List<Vector2> {
            // Outer Triangle Frame
            new Vector2(-60,-50), new Vector2(0,60), new Vector2(60,-50), new Vector2(-60,-50),
            // Transition jump to Inner Triangle Frame
            new Vector2(-30,-30), new Vector2(0,25), new Vector2(30,-30), new Vector2(-30,-30)
        }));

        // --- 2. WATER (Inverted Triangle + Slash Line) ---
        trainingSet.Add(new GestureTemplate("Water", new List<Vector2> {
            new Vector2(-50,40), new Vector2(50,40), new Vector2(0,-50), new Vector2(-50,40), // Inverted Triangle
            new Vector2(-65,0), new Vector2(65,0) // Intersecting Horizontal Crossbar
        }));

        // --- 3. LIGHTNING (Zig-Zag Bolt) ---
        trainingSet.Add(new GestureTemplate("Lightning", new List<Vector2> {
            new Vector2(30,70), new Vector2(-35,15), new Vector2(35,15), new Vector2(-25,-70)
        }));

        // --- 4. EARTH (Diamond Shape Frame + Cross) ---
        trainingSet.Add(new GestureTemplate("Earth", new List<Vector2> {
            new Vector2(0,60), new Vector2(60,0), new Vector2(0,-60), new Vector2(-60,0), new Vector2(0,60), // Diamond Box
            new Vector2(0,60), new Vector2(0,-60),   // Center Vertical Pillar
            new Vector2(-60,0), new Vector2(60,0)     // Center Horizontal Floor
        }));

        // --- 5. WIND (3 Isolated Parallel Stacked Chevrons) ---
        trainingSet.Add(new GestureTemplate("Wind", new List<Vector2> {
            new Vector2(-60,50), new Vector2(0,75), new Vector2(60,50),     // Chevron 1
            new Vector2(-60,10), new Vector2(0,35), new Vector2(60,10),     // Chevron 2
            new Vector2(-60,-30), new Vector2(0,-5), new Vector2(60,-30)    // Chevron 3
        }));
    }
}