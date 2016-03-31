using AcManager.Tools.Objects;
using StringBasedFilter;

namespace AcManager.Tools.Filters {
    public class CarObjectTester : ITester<CarObject> {
        public static CarObjectTester Instance = new CarObjectTester();

        internal static string InnerParameterFromKey(string key) {
            switch (key) {
                case "b":
                case "brand":
                    return nameof(CarObject.Brand);

                case "class":
                    return nameof(CarObject.CarClass);

                case "parent":
                    return nameof(CarObject.Parent);

                case "bhp":
                case "power":
                    return nameof(CarObject.SpecsBhp);

                case "torque":
                    return nameof(CarObject.SpecsTorque);

                case "weight":
                case "mass":
                    return nameof(CarObject.SpecsWeight);

                case "acceleration":
                    return nameof(CarObject.SpecsAcceleration);

                case "speed":
                case "topspeed":
                    return nameof(CarObject.SpecsTopSpeed);

                case "pw":
                case "pwratio":
                    return nameof(CarObject.SpecsPwRatio);

                default:
                    return null;
            }
        }

        public string ParameterFromKey(string key) {
            return InnerParameterFromKey(key) ?? AcJsonObjectTester.InnerParameterFromKey(key);
        }

        public bool Test(CarObject obj, string key, ITestEntry value) {
            switch (key) {
                case "b":
                case "brand":
                    return obj.Brand != null && value.Test(obj.Brand);

                case "class":
                    return obj.CarClass != null && value.Test(obj.CarClass);

                case "parent":
                    return obj.Parent != null && value.Test(obj.Parent.DisplayName);

                case "bhp":
                case "power":
                    return obj.SpecsBhp != null && value.Test(obj.SpecsBhp);

                case "torque":
                    return obj.SpecsTorque != null && value.Test(obj.SpecsTorque);

                case "weight":
                case "mass":
                    return obj.SpecsWeight != null && value.Test(obj.SpecsWeight);

                case "acceleration":
                    return obj.SpecsAcceleration != null && value.Test(obj.SpecsAcceleration);

                case "speed":
                case "topspeed":
                    return obj.SpecsTopSpeed != null && value.Test(obj.SpecsTopSpeed);

                case "pw":
                case "pwratio":
                    return obj.SpecsPwRatio != null && value.Test(obj.SpecsPwRatio);
            }

            return AcJsonObjectTester.Instance.Test(obj, key, value);
        }
    }
}