using UnityEngine;
using System;

namespace SBR {
    public class TypeSelectAttribute : PropertyAttribute {
        public Type baseClass;
        public bool allowAbstract;

        public TypeSelectAttribute(Type baseClass, bool allowAbstract = false) {
            this.baseClass = baseClass;
            this.allowAbstract = allowAbstract;
        }
    }
}