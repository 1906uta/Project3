using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.ServiceModel;

namespace MortgageWorkflow
{
    public class Payments : CodeActivity
    {

        [Input("new_mortgageterm")]
        public InArgument<int> Term { get; set; }
        [Input("new_monthlypayment")]
        public InArgument<decimal> MonthPayment { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


            Entity payment;
            try
            {
                for (int m = 0; m < Term.Get<int>(executionContext); m++)
                {
                    payment = new Entity("new_paymentrecord");
                    payment.Attributes.Add("new_duedate", DateTime.Now.AddMonths(m));
                    payment.Attributes.Add("new_payment", new Money(MonthPayment.Get<decimal>(executionContext)));
                    payment.Attributes.Add("new_mortgage", new EntityReference(context.PrimaryEntityName, context.PrimaryEntityId));
                    service.Create(payment);
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException("An error occurred in Payment Creation Workflow." + ex.Message + "- " + ex.StackTrace, ex);
            }

            catch (Exception ex)
            {
                tracingService.Trace("Payment Creation: {0}" + ex.Message + "- " + ex.StackTrace, ex.ToString());
                throw;
            }
        }
    }
}
