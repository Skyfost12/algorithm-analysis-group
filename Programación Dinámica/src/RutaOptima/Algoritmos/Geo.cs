namespace RutaOptima.Algoritmos {
    public static class Geo {
        private const double RadioTierraMetros = 6_371_000;
        
        public static double Haversine(double lat1, double lng1, double lat2, double lng2) {
            var f1 = lat1 * Math.PI / 180.0;
            var f2 = lat2 * Math.PI / 180.0;
            var df = (lat2 - lat1) * Math.PI / 180.0;
            var dl = (lng2 - lng1) * Math.PI / 180.0;

            var h = Math.Sin(df / 2) * Math.Sin(df / 2) +
                    Math.Cos(f1) * Math.Cos(f2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);

            return RadioTierraMetros * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }
    }
}
