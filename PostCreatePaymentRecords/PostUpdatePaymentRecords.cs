using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
namespace Plugin
{
    public class PostUpdatePaymentRecords : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService =
            (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity config = (Entity)context.InputParameters["Target"];

                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                   if (config.Attributes.Contains("new_value"))
                    {
                        Entity configImage = context.PreEntityImages["preImagineUpdateRecords"];
                        string key = (configImage.Attributes["new_key"].ToString());
                        if(key =="Base APR")
                        {
                            double newAPR= Convert.ToDouble(config.Attributes["new_value"].ToString());
                            decimal amount = 0;
                            double tax = 0;
                            int months=0;
                            tracingService.Trace("Base APR: " + newAPR);

                            //Entity records = new Entity("new_paymentrecords");

                            QueryExpression Query = new QueryExpression("new_paymentrecord");
                            Query.ColumnSet.AddColumn("new_paymentstatus");
                            Query.ColumnSet.AddColumn("new_mortgage");
                            
                            Query.Criteria.AddCondition("new_paymentstatus", ConditionOperator.Equal, 100000000);
                            EntityCollection paymentsC = service.RetrieveMultiple(Query);

                            if (paymentsC.Entities.Count == 0)
                            {
                                tracingService.Trace("none");
                            }
                            else
                            {
                                foreach(Entity pay in paymentsC.Entities)
                                {
                                    tracingService.Trace("payment records " + pay.Attributes["new_mortgage"]);
                                    EntityReference e = (EntityReference)(pay.Attributes["new_mortgage"]);
                                    tracingService.Trace("asdasd: " + e.Id);
                                    Entity mortgageEntity = service.Retrieve(e.LogicalName, e.Id,
                                        new ColumnSet("new_mortgageamount", "new_stateset", "new_country", "new_mortgageterm"));

                                    string country = mortgageEntity.FormattedValues["new_country"].ToString();
                                    months = Int32.Parse(mortgageEntity.Attributes["new_mortgageterm"].ToString());
                                    amount = ((Money)mortgageEntity.Attributes["new_mortgageamount"]).Value;
                                    if (mortgageEntity.Attributes.Contains("new_country")) { 
                                    if(country == "US")
                                    {
                                        if (mortgageEntity.Attributes.Contains("new_stateset"))
                                        {
                                                string state = mortgageEntity.FormattedValues["new_stateset"].ToString();
                                                state = "Tax " + state;
                                                tracingService.Trace(" tax state: " + state);

                                                QueryExpression query = new QueryExpression("new_config");
                                                query.ColumnSet.AddColumn("new_value");
                                                query.Criteria.AddCondition("new_key", ConditionOperator.Equal, state);

                                                EntityCollection taxC = service.RetrieveMultiple(query);
                                                if (taxC.Entities.Count == 0)
                                                {
                                                    throw new InvalidPluginExecutionException("state not found");
                                                }
                                                if (taxC.Entities.Count == 1)
                                                {
                                                    foreach (Entity taxentity in taxC.Entities)
                                                    {

                                                        tax = Convert.ToDouble(taxentity["new_value"].ToString());
                                                    }

                                                }
                                                else
                                                {
                                                    throw new InvalidPluginExecutionException("state not found");
                                                }

                                            }
                                        }
                                    else if(country =="CA")
                                    {
                                            QueryExpression query = new QueryExpression("new_config");
                                            query.ColumnSet.AddColumn("new_value");
                                            query.Criteria.AddCondition("new_key", ConditionOperator.Equal, "Tax Canada");

                                            EntityCollection taxC = service.RetrieveMultiple(query);


                                            if (taxC.Entities.Count == 1)
                                            {
                                                foreach (Entity taxentity in taxC.Entities)
                                                {

                                                    tax = Convert.ToDouble(taxentity["new_value"].ToString());
                                                }

                                            }



                                        }
                                    }
                           
                                    double postamount = (double)amount + ((double)amount * tax);
                                 
                                    double total = (postamount * (newAPR / 12)) / (1 - Math.Pow(1 + (newAPR / 12), (double)(-months)));
                                    decimal totalPerMonth = (decimal)total;
                                    tracingService.Trace("total: " + total);

                                     pay.Attributes["new_payment"] = new Money(totalPerMonth);
                                    service.Update(pay);

                                }
                               
                            }

                        }


                    }
                    else
                    {
                        tracingService.Trace("not new value");
                    }
                    

                    

                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }


    }



}