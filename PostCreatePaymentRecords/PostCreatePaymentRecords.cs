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
    public class PostCreatePaymentsRecords : IPlugin
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
                Entity mortgage = (Entity)context.InputParameters["Target"];

                // Obtain the organization service reference which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                 
                    if (mortgage.Attributes.Contains("new_mortgageterm"))
                    {
                     /*   Guid USABU = new Guid("ba9e5d5d-3eb2-e911-a989-000d3a3acafd");
                        Entity contact = service.Retrieve(c.LogicalName, c.Id,
                    new ColumnSet("firstname", "lastname", "address1_country"));*/

                        //Entity ownerEntity = (Entity)service.Retrieve("systemuser", ownerId, cols);
                        int numberOfMonths = int.Parse(mortgage.Attributes["new_mortgageterm"].ToString());
                        //get the number of months the mortgage is to last for 
                        tracingService.Trace("number of months:" + numberOfMonths);

                        for(int i = 0; i< numberOfMonths; i++)
                        {
                            //create a payment record per month
                            Entity payments = new Entity("new_paymentrecord");
                            //populate the lookup field with the current Mortgage
                            payments.Attributes["new_mortgage"] = mortgage.ToEntityReference();
                            service.Create(payments);
                            
                            
                        }
                        tracingService.Trace("succeeded");
                      
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