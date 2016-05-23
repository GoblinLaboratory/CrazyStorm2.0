﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CrazyStorm.Core
{
    public enum EventOperator : byte
    {
        More,
        Less,
        Equal,
        And,
        Or
    }
    public enum EventKeyword : byte
    {
        ChangeTo,
        Increase,
        Decrease,
        Linear,
        Accelerated,
        Decelerated,
        Fixed
    }
    public class EventInfo
    {
        public bool hasCondition;
        public string leftCondition;
        public string leftOperator;
        public PropertyType leftType;
        public string leftValue;
        public string midOperator;
        public string rightCondition;
        public string rightOperator;
        public PropertyType rightType;
        public string rightValue;
        public bool isSpecialEvent;
        public string property;
        public string changeType;
        public bool isExpressionResult;
        public PropertyType resultType;
        public string resultValue;
        public string changeMode;
        public string changeTime;
        public string executeTime;
        public string specialEvent;
        public string arguments;
    }
    public class EventHelper
    {
        public static string BuildEvent(EventInfo eventInfo)
        {
            string eventString = string.Empty;
            if (eventInfo.hasCondition)
            {
                eventString += string.Format("{0} {1} {2} ", eventInfo.leftCondition, eventInfo.leftOperator, eventInfo.leftValue);
                if (eventInfo.midOperator != null)
                    eventString += string.Format("{0} {1} {2} {3} ", eventInfo.midOperator, eventInfo.rightCondition,
                        eventInfo.rightOperator, eventInfo.rightValue);

                eventString += ": ";
            }
            if (!eventInfo.isSpecialEvent)
            {
                if (eventInfo.isExpressionResult)
                    eventInfo.resultValue = "(" + eventInfo.resultValue + ")";

                eventString += string.Format("{0} {1} {2}, {3}, {4}", eventInfo.property, eventInfo.changeType,
                    eventInfo.resultValue, eventInfo.changeMode, eventInfo.changeTime);
                if (eventInfo.executeTime != null)
                    eventString += ", " + eventInfo.executeTime;
            }
            else
                eventString += string.Format("{0}({1})", eventInfo.specialEvent, eventInfo.arguments);

            eventString += (char)eventInfo.leftType;
            eventString += (char)eventInfo.rightType;
            eventString += (char)eventInfo.resultType;
            return eventString;
        }
        public static EventInfo SplitEvent(string text)
        {
            var info = new EventInfo();
            string[] parts = text.Split(':');
            string eventText = string.Empty;
            if (parts.Length == 2)
            {
                info.hasCondition = true;
                string condition = parts[0];
                string[] split = condition.Split(' ');
                info.leftCondition = split[0];
                info.leftOperator = split[1];
                info.leftValue = split[2];
                if (split.Length == 8)
                {
                    info.midOperator = split[3];
                    info.rightCondition = split[4];
                    info.rightOperator = split[5];
                    info.rightValue = split[6];
                }
                eventText = parts[1].Trim();
            }
            else
            {
                eventText = parts[0].Trim();
            }
            if (eventText.Contains("ChangeTo") || eventText.Contains("Increase") || eventText.Contains("Decrease"))
            {
                string[] split = eventText.Split(' ');
                info.property = split[0];
                info.changeType = split[1];
                info.resultValue = string.Empty;
                split = Regex.Split(eventText, split[1])[1].Split(',');
                for (int i = 0; i < split.Length; ++i)
                {
                    split[i] = split[i].Trim();
                    if (split[i] == "Linear" || split[i] == "Fixed" ||
                        split[i] == "Accelerated" || split[i] == "Decelerated")
                    {
                        info.changeMode = split[i];
                        if (split.Length > i + 2)
                        {
                            info.changeTime = split[i + 1].Trim();
                            string temp = split[i + 2].Trim();
                            info.leftType = (PropertyType)temp[temp.Length - 3];
                            info.rightType = (PropertyType)temp[temp.Length - 2];
                            info.resultType = (PropertyType)temp[temp.Length - 1];
                            info.executeTime = temp.Remove(temp.Length - 3, 3);
                        }
                        else
                        {
                            string temp = split[i + 1].Trim();
                            info.leftType = (PropertyType)temp[temp.Length - 3];
                            info.rightType = (PropertyType)temp[temp.Length - 2];
                            info.resultType = (PropertyType)temp[temp.Length - 1];
                            info.changeTime = temp.Remove(temp.Length - 3, 3);
                        }
                        break;
                    }
                    else
                        info.resultValue += split[i] + ",";
                }
                info.resultValue = info.resultValue.Remove(info.resultValue.Length - 1);
                if (info.resultValue.StartsWith("("))
                {
                    info.isExpressionResult = true;
                    info.resultValue = info.resultValue.Remove(0, 1);
                    info.resultValue = info.resultValue.Remove(info.resultValue.Length - 1);
                }
            }
            else
            {
                info.isSpecialEvent = true;
                string[] split = eventText.Split('(');
                info.specialEvent = split[0];
                split = split[1].Split(')');
                info.arguments = split[0];
                info.leftType = (PropertyType)split[1][0];
                info.rightType = (PropertyType)split[1][1];
                info.resultType = (PropertyType)split[1][2];
            }
            return info;
        }
        public delegate byte[] CompileDelegate(string text);
        public static byte[] GenerateEventData(string text, CompileDelegate compileFunc)
        {
            EventInfo eventInfo = SplitEvent(text);
            Dictionary<string, EventOperator> operatorMap = new Dictionary<string,EventOperator>();
            Dictionary<string, EventKeyword> keywordMap = new Dictionary<string, EventKeyword>();
            operatorMap[">"] = EventOperator.More;
            operatorMap["="] = EventOperator.Equal;
            operatorMap["<"] = EventOperator.Less;
            operatorMap["&"] = EventOperator.And;
            operatorMap["|"] = EventOperator.Or;
            keywordMap["ChangeTo"] = EventKeyword.ChangeTo;
            keywordMap["Increase"] = EventKeyword.Increase;
            keywordMap["Decrease"] = EventKeyword.Decrease;
            keywordMap["Linear"] = EventKeyword.Linear;
            keywordMap["Accelerated"] = EventKeyword.Accelerated;
            keywordMap["Decelerated"] = EventKeyword.Decelerated;
            keywordMap["Fixed"] = EventKeyword.Fixed;
            List<byte> bytes = new List<byte>();
            bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.hasCondition));
            if (eventInfo.hasCondition)
            {
                bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.leftCondition));
                bytes.Add((byte)operatorMap[eventInfo.leftOperator]);
                bytes.Add((byte)eventInfo.leftType);
                bytes.AddRange(PlayDataHelper.GetBytes(PropertyTypeRule.Parse(eventInfo.leftType, eventInfo.leftCondition,
                    eventInfo.leftValue)));
                if (eventInfo.midOperator != null)
                {
                    bytes.Add((byte)operatorMap[eventInfo.midOperator]);
                    bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.rightCondition));
                    bytes.Add((byte)operatorMap[eventInfo.rightOperator]);
                    bytes.Add((byte)eventInfo.rightType);
                    bytes.AddRange(PlayDataHelper.GetBytes(PropertyTypeRule.Parse(eventInfo.rightType, eventInfo.rightCondition, 
                        eventInfo.rightValue)));
                }
                else
                    bytes.Add(0);
            }
            bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.isSpecialEvent));
            if (!eventInfo.isSpecialEvent)
            {
                bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.property));
                bytes.Add((byte)keywordMap[eventInfo.changeType]);
                bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.isExpressionResult));
                bytes.Add((byte)eventInfo.resultType);
                if (eventInfo.isExpressionResult)
                {
                    byte[] compiledExpression = compileFunc(eventInfo.resultValue);
                    bytes.AddRange(PlayDataHelper.GetBytes(compiledExpression.Length));
                    bytes.AddRange(compiledExpression);
                }
                else
                    bytes.AddRange(PlayDataHelper.GetBytes(PropertyTypeRule.Parse(eventInfo.resultType, eventInfo.property, 
                        eventInfo.resultValue)));

                bytes.Add((byte)keywordMap[eventInfo.changeMode]);
                bytes.AddRange(PlayDataHelper.GetBytes(int.Parse(eventInfo.changeTime)));
                if (eventInfo.executeTime != null)
                    bytes.AddRange(PlayDataHelper.GetBytes(int.Parse(eventInfo.executeTime)));
                else
                    bytes.AddRange(PlayDataHelper.GetBytes(0));
            }
            else
            {
                bytes.AddRange(PlayDataHelper.GetBytes(eventInfo.specialEvent));
                string[] split = eventInfo.arguments.Split(',');
                bytes.AddRange(PlayDataHelper.GetBytes(split.Length));
                if (eventInfo.specialEvent == "Loop" && split.Length == 2)
                {
                    bytes.AddRange(PlayDataHelper.GetBytes(split[0]));
                    byte[] compiledExpression = compileFunc(split[1]);
                    bytes.AddRange(PlayDataHelper.GetBytes(compiledExpression.Length));
                    bytes.AddRange(compiledExpression);
                }
                else
                {
                    for (int i = 0; i < split.Length; ++i)
                        bytes.AddRange(PlayDataHelper.GetBytes(split[i]));
                }
            }
            return bytes.ToArray();
        }
    }
}
