using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using MathNet.Numerics.Integration;

public class PDF : MonoBehaviour
{

    public double x = 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate() {
        print("===================== x =====================");
        print($"Normal Dist f, x: {x}, f(x): {normal(x)}");
        print($"CDF: {CDF(x)}");    
    }

    private Func<double, double> normal = z => (1/Math.Sqrt(2*Math.PI)) * Math.Exp(-( Math.Pow(z, 2)/ 2));

    private Func<double, double> integrand = z => Math.Exp(-( Math.Pow(z, 2)/ 2));

    private double CDF(double x) {
        return (1/Mathf.Sqrt(2*Mathf.PI)) * SimpsonRule.IntegrateComposite(integrand, -100, x, 400);
    }

}
