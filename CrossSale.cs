using System;
using System.Collections.Generic;
using System.Text;

using Light.Utils.NetworkTools;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace CrossSale_Forms_Test1
{
    class CrossSale
    {
        public struct ProductPair
        {
            public ProductPair(Guid selectedProduct, Guid proposedProduct, double certainty = 0)
            {
                this.selectedProduct = selectedProduct;
                this.proposedProduct = proposedProduct;
                this.certainty = certainty;
            }

            public readonly Guid selectedProduct;
            public readonly Guid proposedProduct;
            public readonly double certainty;
        }

        private static string baseAddress = "https://api.aihelps.com:8443/v1/";
        private static string accessToken = "441eede5-9996-446d-83f3-c982d98dd790";

        /// <summary>
        /// Sending request to the web-server to recalculate `recommendation` table
        /// </summary>
        public static bool Recalculate() {

            HTTPRequest request = new HTTPRequest(baseAddress + "sales/crosssale/recalculate");
            request.Headers["Authorization"] = "Bearer " + accessToken;

            try {
                request.Get();
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Gets list of ProductPair-s from web-server
        /// </summary>
        /// <param name="clientSex">client's sex: Male / Female</param>
        /// <param name="currentItems">items(products) in the check</param>
        /// <param name="numberOfRecommendations">number of recommendations to retrieve</param>
        /// <param name="proposedProductType">all / services / products</param>
        /// <returns></returns>
        public static List<ProductPair> GetRecommendations(string clientSex, Guid[] currentItems, int numberOfRecommendations, string proposedProductType)
        {
            if (clientSex != "Male" && clientSex != "Female") {
                return null;
            }
            if (numberOfRecommendations < 1 || numberOfRecommendations > 100) {
                return null;
            }
            if (proposedProductType != "all" && proposedProductType != "services" && proposedProductType != "products") {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(baseAddress);
            sb.Append("sales/crosssale/recommendations?");
            sb.AppendFormat("sex={0}&number={1}&proposed_product_type={2}&current_items=", clientSex, numberOfRecommendations, proposedProductType);

            for (int i = 0; i < currentItems.Length; i++) {
                if (currentItems[i].Equals(Guid.Empty)) {
                    return null;
                }
                if (i > 0) {
                    sb.Append(',');
                }
                sb.Append(currentItems[i]);
            }

            return GetRecommendations(sb.ToString());
        }

        /// <summary>
        /// Gets list of ProductPair-s from web-server
        /// </summary>
        /// <param name="clientId">Guid of a client</param>
        /// <param name="currentItems">items(products) in the check</param>
        /// <param name="numberOfRecommendations">number of recommendations to retrieve</param>
        /// <param name="proposedProductType">all / services / products</param>
        /// <returns></returns>
        public static List<ProductPair> GetRecommendations(Guid clientId, Guid[] currentItems, int numberOfRecommendations, string proposedProductType)
        {
            if (clientId.Equals(Guid.Empty)) {
                return null;
            }
            if (numberOfRecommendations < 1 || numberOfRecommendations > 100) {
                return null;
            }
            if (proposedProductType != "all" && proposedProductType != "services" && proposedProductType != "products") {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(baseAddress);
            sb.Append("sales/crosssale/recommendations?");
            sb.AppendFormat("client={0}&number={1}&proposed_product_type={2}&current_items=", clientId, numberOfRecommendations, proposedProductType);

            for (int i = 0; i < currentItems.Length; i++) {
                if (currentItems[i].Equals(Guid.Empty)) {
                    return null;
                }
                if (i > 0) {
                    sb.Append(',');
                }
                sb.Append(currentItems[i]);
            }

            return GetRecommendations(sb.ToString());
        }

        //Sends request to the server and retrieving data
        private static List<ProductPair> GetRecommendations(string URI) {

            HTTPRequest request = new HTTPRequest(URI);
            request.Headers["Authorization"] = "Bearer " + accessToken;
            string response = "";
            try {
                response = request.Get();
            }
            catch (Exception) {
                return null;
            }

            List<ProductPair> result;
            if (TryDeserializeJson(response, out result)) {
                return result;
            }
            return null;
        }

        //Trying to deserialize response. If exception - return false
        private static bool TryDeserializeJson<T>(string jsonString, out T returnParameter) {

            try {
                returnParameter = JsonConvert.DeserializeObject<T>(jsonString);
                return true;
            }
            catch (Exception) {
                // handle exception
                returnParameter = default(T);
                return false;
            }
        }

        /// <summary>
        /// Reporting about successes and failures to the web-server
        /// </summary>
        /// <param name="clientSex">client's sex</param>
        /// <param name="report">key: ProductPair without certainty; value: true if ProductPair has been added to the check, false - else</param>
        public static void ReportSuccess(string clientSex, Dictionary<ProductPair, bool> report) {

            HTTPRequest request = new HTTPRequest(baseAddress + "sales/crosssale/report_success");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "Bearer " + accessToken;

            StringBuilder content = new StringBuilder();
            content.Append('[');
            foreach (var kvp in report) {
                content.Append('{');
                content.AppendFormat(" \"sex\" : \"{0}\",", clientSex);
                content.AppendFormat(" \"selected_product\" : {0}", kvp.Key.selectedProduct);
                content.AppendFormat(" \"proposed_product\" : {0}", kvp.Key.proposedProduct);
                content.AppendFormat(" \"success\" : {0}", kvp.Value);
                content.Append("},");
            }
            content.Remove(content.Length - 1, 1);
            content.Append(']');

            request.Content = content.ToString();
            try {
                request.Post();
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString());
            }
        }
    }
}