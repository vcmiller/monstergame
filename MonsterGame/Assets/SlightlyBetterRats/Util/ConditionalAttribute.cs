using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attribute that tells Unity to only draw a property when a given condition is true. The condition is the name of a parameterless function, field, or property of type bool.
/// </summary>
public class ConditionalAttribute : PropertyAttribute {
    public ConditionalAttribute(string condition) {
        this.condition = condition;
    }

    public readonly string condition;
}
