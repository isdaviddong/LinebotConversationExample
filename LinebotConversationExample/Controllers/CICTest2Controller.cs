using isRock.LineBot.Conversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LinebotConversationExample.Controllers
{
    public class CICTest2Controller : ApiController
    {
        [HttpPost]
        public IHttpActionResult POST()
        { 
            string ChannelAccessToken = "eu93Yv4pxcz8pTTypG61p6RUHMnXdr0ElrE7FXOBFFgeKL/8wvmgdKpA9j1Akl3eP2YwHc/34q8GTZK5NC+zBDCRzbXCMrsdCiW87gVZZYeV/NkwtaQzpbQY6JDkD/4ELHI9UoX6+UT8WH7OvOruyAdB04t89/1O/w1cDnyilFU=";
            var responseMsg = "";

            try
            {
                //定義資訊蒐集者
                isRock.LineBot.Conversation.InformationCollector<LeaveRequestV2> CIC =
                    new isRock.LineBot.Conversation.InformationCollector<LeaveRequestV2>(ChannelAccessToken);
                CIC.OnMessageTypeCheck += (s, e) => {
                    switch (e.CurrentPropertyName)
                    {
                        case "代理人":
                            if (e.ReceievedMessage != "eric")
                            {
                                e.isMismatch = true;
                                e.ResponseMessage = "我們公司只有eric，代理人請找eric...";
                            }
                            break;
                        case "假別":
                            if (e.ReceievedMessage != "事假" && e.ReceievedMessage != "病假" && e.ReceievedMessage != "公假")
                            {
                                e.isMismatch = true;
                                e.ResponseMessage = "你只能輸入事假,病假,事假其中之一";
                            }
                            break;
                        default:
                            break;
                    }

                };

                //取得 http Post RawData(should be JSO
                string postData = Request.Content.ReadAsStringAsync().Result;
                //剖析JSON
                var ReceivedMessage = isRock.LineBot.Utility.Parsing(postData);
                //定義接收CIC結果的類別
                ProcessResult<LeaveRequestV2> result;
                if (ReceivedMessage.events[0].message.text == "我要請假")
                {
                    //把訊息丟給CIC 
                    result = CIC.Process(ReceivedMessage.events[0], true);
                    responseMsg = "開始請假程序\n";
                }
                else
                {
                    //把訊息丟給CIC 
                    result = CIC.Process(ReceivedMessage.events[0]);
                }

                //處理 CIC回覆的結果
                switch (result.ProcessResultStatus)
                {
                    case ProcessResultStatus.Processed:
                        if (result.ResponseButtonsTemplateCandidate != null)
                        {
                            //如果有template Message，直接回覆，否則放到後面一起回覆
                            isRock.LineBot.Utility.ReplyTemplateMessage(
                                ReceivedMessage.events[0].replyToken,
                                result.ResponseButtonsTemplateCandidate,
                                ChannelAccessToken);
                            return Ok();
                        }
                        //取得候選訊息發送
                        responseMsg += result.ResponseMessageCandidate;
                        break;
                    case ProcessResultStatus.Done:
                        responseMsg += result.ResponseMessageCandidate;
                        responseMsg += $"蒐集到的資料有...\n";
                        responseMsg += Newtonsoft.Json.JsonConvert.SerializeObject(result.ConversationState.ConversationEntity);
                        break;
                    case ProcessResultStatus.Pass:
                        responseMsg = $"你說的 '{ReceivedMessage.events[0].message.text}' 我看不懂，如果想要請假，請跟我說 : 『我要請假』";
                        break;
                    case ProcessResultStatus.Exception:
                        //取得候選訊息發送
                        responseMsg += result.ResponseMessageCandidate;
                        break;
                    case ProcessResultStatus.Break:
                        //取得候選訊息發送
                        responseMsg += result.ResponseMessageCandidate;
                        break;
                    case ProcessResultStatus.InputDataFitError:
                        responseMsg += "\n資料型態不合\n";
                        responseMsg += result.ResponseMessageCandidate;
                        break;
                    default:
                        //取得候選訊息發送
                        responseMsg += result.ResponseMessageCandidate;
                        break;
                }
                //回覆用戶訊息
                isRock.LineBot.Utility.ReplyMessage(ReceivedMessage.events[0].replyToken, responseMsg, ChannelAccessToken);
                //回覆API OK
                return Ok();
            }
            catch (Exception ex)
            {
                //如果你要偵錯的話
                isRock.LineBot.Utility.PushMessage("eu93Yv4pxcz8pTTypG61p6RUHMnXdr0ElrE7FXOBFFgeKL/8wvmgdKpA9j1Akl3eP2YwHc/34q8GTZK5NC+zBDCRzbXCMrsdCiW87gVZZYeV/NkwtaQzpbQY6JDkD/4ELHI9UoX6+UT8WH7OvOruyAdB04t89/1O/w1cDnyilFU=", ex.Message, ChannelAccessToken);
                return Ok();
                throw ex;
            }
        }
    }

    /// <summary>
    /// 用來表達一個對話
    /// </summary>
    public class LeaveRequestV2 : ConversationEntity
    {
        [ButtonsTemplateQuestion("詢問", "請問您要請的假別是?", "https://arock.blob.core.windows.net/blogdata201706/22-124357-ad3c87d6-b9cc-488a-8150-1c2fe642d237.png", "事假", "病假", "公假", "婚假")]
        [Order(1)]
        public string 假別 { get; set; }

        [Question("請問您的代理人是誰?")]
        [Order(2)]
        public string 代理人 { get; set; }

        [Question("請問您的請假日期是?")]
        [Order(3)]
        public DateTime 請假日期 { get; set; }

        [Question("請問您的開始時間是幾點幾分?")]
        [Order(4)]
        public DateTime 開始時間 { get; set; }

        [Question("請問您要請幾小時?")]
        [Order(5)]
        public float 請假時數 { get; set; }
    }
}
