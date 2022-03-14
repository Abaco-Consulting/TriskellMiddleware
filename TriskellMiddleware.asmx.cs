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
using System.Xml;
using System.Xml.Serialization;
using System.IO;

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
        // WebService GetApprovedTimesheetsByDateRange
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
                                "'valuesByParams':'" + reportParams + "'" +
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

        [WebMethod]
        // Webservice - Criar projecto em Triskell
        public CreateDataProjectReturn CreateDataProject()
        {
            // criar o objecto da resposta
            CreateDataProjectReturn oReturn = new CreateDataProjectReturn();

            // primeiro há que invocar a API do triskell para efetuar o login com um user API REST
            var authentication = new TriskellAuthentication
            {
                user = System.Configuration.ConfigurationManager.AppSettings["Triskell_API_Username"].ToString(),
                password = Triskell.ComputeSha256Hash(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_Password"].ToString())
            };

            var httpClient = new HttpClient();

            var httpContent = new StringContent(JsonConvert.SerializeObject(authentication), Encoding.UTF8, "application/json");

            //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            HttpResponseMessage httpResponse = httpClient.PostAsync(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_LoginJSON"].ToString(), httpContent).Result;

            if (httpResponse.Content != null)
            {
                string jsonContent = httpResponse.Content.ReadAsStringAsync().Result.ToString();

                if (httpResponse.IsSuccessStatusCode) // success
                {
                    //XML do INPUT 

                    string GetProjectDataXMLRequest =
                        "<dataobjects>" +
                            "<dataobject>" +
                                "<object>Project</object>" +
                                "<name>500</name>" +
                                "<description>Project X Description</description>" +
                                "<stage>1.Sale</stage>" +
                                "<pool>OWN</pool>" +
                                "<parentid>1025</parentid>" +
                                "<currency>5</currency>" +
                                "<attributes>" +
                                "<attribute>" +
                                    "<name>Company</name>" +
                                    "<value>1000</value>" +
                                "</attribute>" +
                                "<attribute>" +
                                    "<name>Products</name>" +
                                    "<value>SAP</value>" +
                                 "</attribute>" +
                                 "<attribute>" +
                                    "<name>Profit Center</name>" +
                                    "<value>PT10101001</value>" +
                                 "</attribute>" +
                                 "<attribute>" +
                                    "<name>Project Group</name>" +
                                    "<value>Customer</value>" +
                                 "</attribute>" +
                                 "<attribute>" +
                                    "<name>Client</name>" +
                                    "<value>100818</value>" +
                                 "</attribute>" +
                                 "<attribute>" +
                                    "<name>Contract Type</name>" +
                                    "<value>PF</value>" +
                                 "</attribute>" +
                                 "<attribute>" +
                                    "<name>Business Unit</name>" +
                                    "<value>AMS</value>" +
                                 "</attribute>" +
                                 "</attributes>" +
                                 "<user_roles>" +
                                    "<user_role>" +
                                        "<user_code>HAA</user_code>" +
                                        "<role>Controller</role>" +
                                    "</user_role>" +
                                    "<user_role>" +
                                        "<user_code>VEC</user_code>" +
                                        "<role>Project Manager</role>" +
                                    "</user_role>" +
                                    "<user_role>" +
                                        "<user_code>JCC</user_code>" +
                                        "<role>Project Director</role>" +
                                    "</user_role>" +
                                 "</user_roles>" + 
                                "</dataobject>" +
                                "</dataobjects>";

                    // Pedido no formato XML
                    httpContent = new StringContent(GetProjectDataXMLRequest, Encoding.UTF8, "text/xml");

                    httpResponse = httpClient.PostAsync(System.Configuration.ConfigurationManager.AppSettings["Triskell_API_DataobjectCreate"].ToString(), httpContent).Result;

                    if (httpResponse.Content != null)
                    {
                        var GetProjectDataXMLResponse = httpResponse.Content.ReadAsStringAsync();

                        if (httpResponse.IsSuccessStatusCode) // success
                        {

                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(GetProjectDataXMLResponse.Result);
                            XmlElement root = doc.DocumentElement;

                            // validar se a resposta contém dados xml no formato <dataobjects>
                            if (root.LocalName == "dataobjects")
                            {
                                XmlNodeList xmlResult = root.GetElementsByTagName("result");
                                string sResult = xmlResult[0].InnerText;

                                if (sResult == "OK")
                                {
                                    XmlNodeList xmlProjectName = root.GetElementsByTagName("name");
                                    string sProjectName = xmlProjectName[0].InnerText;
                                    XmlNodeList xmlProjectTriskellId = root.GetElementsByTagName("dataobjectid");
                                    string sProjectTriskellId = xmlProjectTriskellId[0].InnerText;

                                    oReturn.SuccessMsg = "Projecto '" + sProjectName + "' criado com id '" + sProjectTriskellId + "'";
                                    oReturn.Error = false;
                                }
                                else
                                {
                                    XmlNodeList xmlError = root.GetElementsByTagName("error");
                                    string sError = xmlError[0].InnerText;

                                    oReturn.Error = true;
                                    oReturn.ErrorMsg = sError;
                                }
                            }
                            else
                            {
                                oReturn.Error = true;
                                oReturn.ErrorMsg = "Incoerência na autenticação e chamada do método. Verificar configurações do webservice";
                            }
                        
                            }
                    else
                    {
                        oReturn.Error = true;
                        oReturn.ErrorMsg = "Erro ao invocar a criação de um projeto";
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
