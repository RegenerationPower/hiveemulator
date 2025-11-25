namespace DevOpsProject.Bms.Logic.Options
{
    public class BmsMonitoringOptions
    {
        // Мінімальна допустима дистанція між Hive (км)
        public double MinDistanceBetweenHivesKm { get; set; } = 1.0;

        // Приблизна кількість км на градус широти/довготи (для грубої евклідової оцінки)
        public double KmPerDegree { get; set; } = 111.0;
    }
}