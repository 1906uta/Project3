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
    public class PostAssignMortgage : IPlugin
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
                    //check if it has the field 
                    if (mortgage.Attributes.Contains("new_country"))
                    {
                        tracingService.Trace("inside country");
                        string country = ((mortgage.FormattedValues["new_country"]).ToString());
                        //get the two teams
                        Guid USTeam = new Guid("df9c2d57-e1b2-e911-a989-000d3a3acafd");
                        Guid CATeam = new Guid("b7bf485d-e1b2-e911-a989-000d3a3acafd");

                        if (country == "US")
                        {
                            tracingService.Trace("inside USA");
                            int curr = 0;
                            int nusers = 0;
                            //get all the users here
                            QueryExpression userQuery = new QueryExpression("systemuser");
                            userQuery.ColumnSet = new ColumnSet(true);
                            LinkEntity teamLink = new LinkEntity("systemuser", "teammembership", "systemuserid", "systemuserid", JoinOperator.Inner);
                            ConditionExpression teamCondition = new ConditionExpression("teamid", ConditionOperator.Equal, USTeam);
                            teamLink.LinkCriteria.AddCondition(teamCondition);
                            userQuery.LinkEntities.Add(teamLink);

                            EntityCollection retrievedUsers = service.RetrieveMultiple(userQuery);
                            var userlist = new List<Entity>();
                            if (retrievedUsers.Entities.Count == 0)
                            {
                                //if no users then this
                                throw new InvalidPluginExecutionException("no users found in Owning Team");

                            }
                            else
                            {
                                //if there is users then this
                                tracingService.Trace("users in USA team");
                                foreach (Entity user in retrievedUsers.Entities)
                                {
                                    //add the users to a List 
                                    var userId = user.Id;
                                    var current = user.Contains("fullname") ? user["fullname"].ToString() : "";
                                    userlist.Add(user);
                                    // tracingService.Trace("name of users " + current);
                                    nusers++;
                                }

                            }
                            //get the current US counter

                            QueryExpression query = new QueryExpression("new_config");
                            query.ColumnSet.AddColumn("new_value");
                            query.Criteria.AddCondition("new_key", ConditionOperator.Equal, "UScounter");

                            EntityCollection counter = service.RetrieveMultiple(query);
                            if (counter.Entities.Count == 0)
                            {
                                //if empty then make one
                                Entity newcount = new Entity("new_config");
                                newcount.Attributes.Add("new_key", "UScounter");
                                newcount.Attributes.Add("new_value", (0.ToString()));
                                service.Create(newcount);
                                tracingService.Trace("counter is empty");
                            }

                            if (counter.Entities.Count == 1)
                            {
                                foreach (Entity count in counter.Entities)
                                {
                                    var count1 = count.Contains("new_value") ? count["new_value"].ToString() : "";
                                    curr = Int32.Parse(count1);
                                    int next = Int32.Parse(count1);
                                    next = next + 1;
                                    if (next + 1 > nusers)
                                    {
                                        next = 0;
                                    }
                                    //get the current "count" and set the next one
                                    tracingService.Trace("next: " + next);
                                    count["new_value"] = next.ToString();

                                    service.Update(count);
                                }
                            }
                            //change the ownership
                           
                            tracingService.Trace("current user" + userlist[curr]);
                            mortgage.Attributes["ownerid"] = userlist[curr].ToEntityReference();
                            mortgage.EntityState = EntityState.Changed;
                             service.Update(mortgage);
                        }

                        if (country == "CA")
                        {
                            //if Canada then this 
                            tracingService.Trace("inside CA");
                            int curr = 0;
                            int nusers = 0;
                            //get users 
                            QueryExpression userQuery = new QueryExpression("systemuser");
                            userQuery.ColumnSet = new ColumnSet(true);
                            LinkEntity teamLink = new LinkEntity("systemuser", "teammembership", "systemuserid", "systemuserid", JoinOperator.Inner);
                            ConditionExpression teamCondition = new ConditionExpression("teamid", ConditionOperator.Equal, CATeam);
                            teamLink.LinkCriteria.AddCondition(teamCondition);
                            userQuery.LinkEntities.Add(teamLink);

                            EntityCollection retrievedUsers = service.RetrieveMultiple(userQuery);
                            var userlist = new List<Entity>();
                            if (retrievedUsers.Entities.Count == 0)
                            {

                                throw new InvalidPluginExecutionException("no users found in Owning Team");

                            }
                            else
                            {
                                //add users to list
                                tracingService.Trace("users in CA team");
                                foreach (Entity user in retrievedUsers.Entities)
                                {
                                    var userId = user.Id;
                                    var current = user.Contains("fullname") ? user["fullname"].ToString() : "";
                                    userlist.Add(user);
                                    //  tracingService.Trace("name of users " + current);
                                    nusers++;
                                }

                            }
                            //get Canada counter
                            QueryExpression query = new QueryExpression("new_config");
                            query.ColumnSet.AddColumn("new_value");
                            query.Criteria.AddCondition("new_key", ConditionOperator.Equal, "CAcounter");

                            EntityCollection counter = service.RetrieveMultiple(query);
                            if (counter.Entities.Count == 0)
                            {
                                Entity newcount = new Entity("new_config");
                                newcount.Attributes.Add("new_key", "CAcounter");
                                newcount.Attributes.Add("new_value", (0.ToString()));
                                service.Create(newcount);
                                tracingService.Trace("counter is empty");
                            }
                            if (counter.Entities.Count == 1)
                            {
                                foreach (Entity count in counter.Entities)
                                {
                                    var count1 = count.Contains("new_value") ? count["new_value"].ToString() : "";
                                    curr = Int32.Parse(count1);
                                    int next = Int32.Parse(count1);
                                    next = next + 1;
                                    if (next + 1 > nusers)
                                    {
                                        next = 0;
                                    }

                                    tracingService.Trace("next: " + next);
                                    count["new_value"] = next.ToString();
                                    mortgage.EntityState = EntityState.Changed;
                                    service.Update(count);
                                }
                            }
                            //Assing users 
                           // tracingService.Trace("current user" + userlist[curr].Attributes["firstname"]);
                           // mortgage.Attributes["ownerid"] = userlist[curr].ToEntityReference();
                           // service.Update(mortgage);
                            tracingService.Trace("last");
                        }


                    }
                    //MIGHT MOVE IT TO ITS OWN PLUG IN
                    /*
                    if (mortgage.Attributes.Contains("new_country"))
                    {
                        string country = ((mortgage.FormattedValues["new_country"]).ToString());
                        int months = Int32.Parse(mortgage.Attributes["new_mortgageterm"].ToString());

                        decimal amount = ((Money)mortgage.Attributes["new_mortgageamount"]).Value;
                        double tax = 0;
                        double apr = 0;
                        if (country == "US")
                        {
                            if (mortgage.Attributes.Contains("new_stateset"))
                            {
                              
                               
                                string state = mortgage.FormattedValues["new_stateset"].ToString();
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
                                QueryExpression APRquery = new QueryExpression("new_config");
                                APRquery.ColumnSet.AddColumn("new_value");
                                APRquery.Criteria.AddCondition("new_key", ConditionOperator.BeginsWith, "Base APR");

                                EntityCollection APRC = service.RetrieveMultiple(APRquery);

                                if (APRC.Entities.Count > 0)
                                {
                                    foreach(Entity aprentity in APRC.Entities)
                                    {
                                      
                                       apr = Convert.ToDouble(aprentity["new_value"].ToString());
                                    }
                                    
                                }
                                else
                                {
                                    tracingService.Trace("none found");
                                }
                               
                            }
                        }else if(country == "CA")
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
                            QueryExpression APRquery = new QueryExpression("new_config");
                            APRquery.ColumnSet.AddColumn("new_value");
                            APRquery.Criteria.AddCondition("new_key", ConditionOperator.BeginsWith, "Base APR");

                            EntityCollection APRC = service.RetrieveMultiple(APRquery);

                            if (APRC.Entities.Count > 0)
                            {
                                foreach (Entity aprentity in APRC.Entities)
                                {

                                    apr = Convert.ToDouble(aprentity["new_value"].ToString());
                                }

                            }
                            else
                            {
                                tracingService.Trace("none found");
                            }

                        }

                        else
                        {
                            throw new InvalidPluginExecutionException("country not found");
                        }
                       // tax = tax * 100;
                        //apr = apr * 100;
                        tracingService.Trace("tax " + tax);
                        tracingService.Trace("apr " + apr);
                        double postamount = (double)amount + ((double)amount * tax);

                        double total = (postamount * (apr / 12)) / (1 - Math.Pow(1 + (apr / 12), (double)(-months)));
                        decimal totalPerMonth = (decimal)total;
                        tracingService.Trace("total: " + total);

                       // mortgage.Attributes.Add("new_monthlypayment", new Money(totalPerMonth));
                        service.Update(mortgage);
                    }*/
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