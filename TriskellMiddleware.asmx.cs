using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Services;
using TriskellMiddleware.Classes;
using TriskellMiddleware.Helpers;
using Newtonsoft.Json;
using System.Text;

namespace TriskellMiddleware
{
    /// <summary>
    /// Summary description for TriskellMiddleware
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class TriskellMiddleware : System.Web.Services.WebService
    {

        [WebMethod]
        //date in format AAAAMMDD
        public GetApprovedTimesheetsByDateRangeReturn GetApprovedTimesheetsByDateRange(string dateFrom, string dateTo)
        {

            //criar o objecto da resposta
            GetApprovedTimesheetsByDateRangeReturn oReturn = new GetApprovedTimesheetsByDateRangeReturn();

            #region Validation

            if (dateFrom == null || dateFrom == "" || dateTo == null || dateTo == "")
            {
                oReturn.Error = true;
                oReturn.ErrorMsg = "Indicar a data início e/ou a data fim";
                return oReturn;
            }
            else if(dateFrom.Length != 8 || !dateFrom.All(Char.IsDigit) || dateTo.Length != 8 || !dateTo.All(Char.IsDigit))
            {
                oReturn.Error = true;
                oReturn.ErrorMsg = "A data início e/ou a data fim tem que estar no formato AAAAMMDD";
                return oReturn;
            }

            DateTime dtDateFrom, dtDateTo;

            try
            {
                dtDateFrom = new DateTime(int.Parse(dateFrom.Substring(0, 4)), int.Parse(dateFrom.Substring(4, 2)), int.Parse(dateFrom.Substring(6, 2)));

                dtDateTo = new DateTime(int.Parse(dateTo.Substring(0, 4)), int.Parse(dateTo.Substring(4, 2)), int.Parse(dateTo.Substring(6, 2)));
            }
            catch(Exception e)
            {
                oReturn.Error = true;
                oReturn.ErrorMsg = "A data de início e/ou a data fim não é uma data válida";
                return oReturn;
            }

            #endregion


            //será a lista de Timesheet a devolver
            List<Timesheet> timesheets = new List<Timesheet>();

            // primeiro há que invocar a API do triskell para efetuar o login com um user API REST
            var authentication = new TriskellAuthentication { user =  System.Configuration.ConfigurationManager.AppSettings["Triskell_API_Username"].ToString(),
                                                              password = Triskell.ComputeSha256Hash(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_Password"].ToString()) };

            var httpClient = new HttpClient();

            var httpContent = new StringContent(JsonConvert.SerializeObject(authentication), Encoding.UTF8, "application/json");

            //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            HttpResponseMessage httpResponse = httpClient.PostAsync(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_LoginJSON"].ToString(), httpContent).Result;

            if (httpResponse.Content != null)
            {
                string jsonContent = httpResponse.Content.ReadAsStringAsync().Result.ToString();

                if (httpResponse.IsSuccessStatusCode) // success
                {
                    //se a autenticação teve sucesso há que chamar o método que retorna o resultado do report dos RA

                    string reportId = "15";     //o ID do report -> obtido no interface do Triskell
                    string reportParams = "";   // os parametros do report

                    //Data_Inicio -> ID 6 -> Formato de Input: YYYY.MM.DD
                    //Data_Fim -> ID 7  -> Formato de Input: YYYY.MM.DD
                    //Status_Timesheet -> ID 8 -> Texto do Status da Timesheet

                    #region construção da string dos parametros

                    //Data_Inicio
                    reportParams += "6#" + dtDateFrom.Year.ToString() + "." + dtDateFrom.Month.ToString() + "." + dtDateFrom.Day.ToString();

                    //Separador
                    reportParams += "##";

                    //Data_Fim
                    reportParams += "7#" + dtDateTo.Year.ToString() + "." + dtDateTo.Month.ToString() + "." + dtDateTo.Day.ToString();

                    //Separador
                    reportParams += "##";

                    reportParams += "8#Approved";


                    #endregion


                    //JSON do INPUT 

                    string GetReportDataToPanelJSONRequest =
                        "{" +
                            "'id' : 0," +               //fixo, não mexer
                            "'params' : {" +
                                "'REPORTID':'" + reportId + "'," +             
                                "'valuesByParams':'" + reportParams  + "'" +    
                            "}," +
                            "'objects' : null" +        //fixo, não mexer
                        "}";

                    httpContent = new StringContent(GetReportDataToPanelJSONRequest, Encoding.UTF8, "application/json");

                    //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                    httpResponse = httpClient.PostAsync(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_GetReportDataToPanel"].ToString(), httpContent).Result;

                    if (httpResponse.Content != null)
                    {
                        var GetReportDataToPanelJSONResponse = httpResponse.Content.ReadAsStringAsync();

                        if (httpResponse.IsSuccessStatusCode) // success
                        {
                            dynamic GetReportDataToPanelObjectResponse = JsonConvert.DeserializeObject(GetReportDataToPanelJSONResponse.Result.ToString());

                            if(GetReportDataToPanelObjectResponse.success.Value)
                            {
                                var RAResults = GetReportDataToPanelObjectResponse.data[System.Configuration.ConfigurationManager.AppSettings["Triskell_API_StoredSelector_GetApprovedTimesheetsByDateRange"].ToString()].res;

                                oReturn.Timesheets = RAResults.ToObject<List<Timesheet>>();
                                oReturn.Error = false;
                            }
                            else
                            {
                                oReturn.Error = true;
                                oReturn.ErrorMsg = "Triskell informa erro ao correr o report '" + reportId + "' com os parametros '" + reportParams + "'";
                            }
                        }
                        else
                        {
                            oReturn.Error = true;
                            oReturn.ErrorMsg = "Erro ao invocar o report '" + reportId + "' com os parametros '" + reportParams + "'";
                        }
                    }
                }
                else
                {
                    oReturn.Error = true;
                    oReturn.ErrorMsg = "Erro no login em Triskell com o user " + authentication.user + " (user Triskell deverá ser do tipo REST API Client)";
                }

            }

            return oReturn;

        }
    }
}
