using KCM.Attributes;
using KCM.Packets.Lobby;
using KCM.Packets.Network;
using Riptide;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Handlers
{
    public class PacketHandler
    {
        [ThreadStatic]
        private static bool isHandlingPacket;

        public static bool IsHandlingPacket
        {
            get { return isHandlingPacket; }
        }

        public static Dictionary<ushort, PacketRef> Packets = new Dictionary<ushort, PacketRef>();
        public class PacketRef
        {
            public IPacket packet;
            public PropertyInfo[] properties;

            public PacketRef(IPacket packet, PropertyInfo[] properties)
            {
                this.packet = packet;
                this.properties = properties;
            }
        }


        public static Dictionary<ushort, PacketHandlerDelegate> PacketHandlers = new Dictionary<ushort, PacketHandlerDelegate>();
        public delegate void PacketHandlerDelegate(IPacket packet);

        public static void Initialise()
        {
            try
            {
                Main.helper.Log("Loading Packet Handlers...");

                //TO-DO Remove this. Packets now have "handle packet" method
                #region "Register server packet handlers"

                var serverPacketHandlers = Assembly.GetExecutingAssembly().GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    .Where(m => m.GetCustomAttributes(typeof(PacketHandlerAttribute), false).Length > 0)
                    .ToArray();

                foreach (MethodInfo method in serverPacketHandlers)
                {
                    PacketHandlerAttribute attribute = method.GetCustomAttribute<PacketHandlerAttribute>();


                    if (!method.IsStatic)
                        throw new NonStaticHandlerException(method.DeclaringType, method.Name);

                    Delegate packetHandler = Delegate.CreateDelegate(typeof(PacketHandlerDelegate), method, false);
                    if (packetHandler != null)
                    {
                        // It's a message handler for Client instances
                        if (PacketHandlers.ContainsKey(attribute.packetId))
                        {
                            MethodInfo otherMethodWithId = PacketHandlers[attribute.packetId].GetMethodInfo();
                            throw new DuplicateHandlerException(attribute.packetId, method, otherMethodWithId);
                        }
                        else
                            PacketHandlers.Add(attribute.packetId, (PacketHandlerDelegate)packetHandler);
                    }
                    else
                    {
                        Main.helper.Log($"Failed to register handler: {method.Name}");
                    }
                }

                Main.helper.Log($"Loaded {PacketHandlers.Count} server handlers");

                #endregion


                Main.helper.Log("Loading packets...");

                var packets = Assembly.GetExecutingAssembly().GetTypes().Where(t => t != null && t.Namespace != null && t.Namespace.StartsWith("KCM.Packets") && !t.IsAbstract && !t.IsInterface && typeof(IPacket).IsAssignableFrom(t)).ToList();

                foreach (var packet in packets)
                {

                    IPacket p = (IPacket)Activator.CreateInstance(packet);
                    var properties = packet.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(prop => prop.Name != "packetId" && prop.Name != "sendMode")
                        .ToArray();
                    Array.Sort(properties, (x, y) => String.Compare(x.Name, y.Name));
                    ushort id = (ushort)p.GetType().GetProperty("packetId").GetValue(p, null);

                    if (p.GetType() == typeof(SaveTransferPacket))
                    {
                        Main.helper.Log("SaveTransferPacket");
                        Main.helper.Log(string.Join("\n", properties.Select(x => x.Name).ToArray()));
                    }

                    Packets.Add(id, new PacketRef(p, properties));
                    Main.helper.Log($"- Registered Packet: {id} {packet.FullName}");

                }

                Main.helper.Log($"Loaded {Packets.Count} packets");
            }
            catch (Exception ex)
            {
                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Main.helper.Log("----------------------- Inner exception -----------------------");
                    Main.helper.Log(ex.InnerException.ToString());
                    Main.helper.Log("----------------------- Inner message -----------------------");
                    Main.helper.Log(ex.InnerException.Message);
                    Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                    Main.helper.Log(ex.InnerException.StackTrace);
                }
            }
        }

        public static void HandlePacketServer(object sender, MessageReceivedEventArgs messageReceived)
        {
            var id = messageReceived.MessageId;


            IPacket packet = DeserialisePacket(messageReceived);

            //Main.helper.Log($"Server Received packet {Packets[id].packet.GetType().Name} from {messageReceived.FromConnection.Id}");


            if (KCServer.IsRunning)
            {
                try
                {
                    packet.HandlePacketServer();

                    bool shouldRelay = packet.GetType().GetCustomAttributes(typeof(NoServerRelayAttribute), inherit: true).Length == 0;
                    if (shouldRelay)
                        ((Packet)packet).SendToAll();
                }
                catch (Exception ex)
                {
                    Main.helper.Log($"Error handling packet {id} {packet.GetType().Name} from {packet.clientId}");

                    Main.helper.Log("----------------------- Main exception -----------------------");
                    Main.helper.Log(ex.ToString());
                    Main.helper.Log("----------------------- Main message -----------------------");
                    Main.helper.Log(ex.Message);
                    Main.helper.Log("----------------------- Main stacktrace -----------------------");
                    Main.helper.Log(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        Main.helper.Log("----------------------- Inner exception -----------------------");
                        Main.helper.Log(ex.InnerException.ToString());
                        Main.helper.Log("----------------------- Inner message -----------------------");
                        Main.helper.Log(ex.InnerException.Message);
                        Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                        Main.helper.Log(ex.InnerException.StackTrace);
                    }
                }
            }
        }

        public static void HandlePacket(object sender, MessageReceivedEventArgs messageReceived)
        {
            try
            {
                var id = messageReceived.MessageId;


                //Main.helper.Log($"Client Received packet {Packets[id].packet.GetType().Name} from {messageReceived.FromConnection.Id}");

                IPacket packet = DeserialisePacket(messageReceived);

                //Main.helper.Log($"Client Received packet {Packets[id].packet.GetType().Name} from {packet.clientId}");

                if (KCClient.client.IsConnected)
                {
                    try
                    {
                        isHandlingPacket = true;
                        packet.HandlePacketClient();
                    }
                    catch (Exception ex)
                    {
                        Main.helper.Log($"Error handling packet {id} {packet.GetType().Name} from {packet.clientId}");

                        Main.helper.Log("----------------------- Main exception -----------------------");
                        Main.helper.Log(ex.ToString());
                        Main.helper.Log("----------------------- Main message -----------------------");
                        Main.helper.Log(ex.Message);
                        Main.helper.Log("----------------------- Main stacktrace -----------------------");
                        Main.helper.Log(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            Main.helper.Log("----------------------- Inner exception -----------------------");
                            Main.helper.Log(ex.InnerException.ToString());
                            Main.helper.Log("----------------------- Inner message -----------------------");
                            Main.helper.Log(ex.InnerException.Message);
                            Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                            Main.helper.Log(ex.InnerException.StackTrace);
                        }
                    }
                    finally
                    {
                        isHandlingPacket = false;
                    }
                }

                /* if (PacketHandlers.TryGetValue(id, out PacketHandlerDelegate handler))
                     handler(packet);*/

                // Main.helper.Log($"{(KCServer.IsRunning ? "Server" : "Client")} Received packet {id} {packet.GetType().kingdomName}");
                //Main.helper.Log($"Found handler: {(handler != null).ToString()}");
            }
            catch
            {

            }
        }

        public static Message SerialisePacket(IPacket packet)
        {

            var currentPropName = "";
            try
            {
                var packetRef = Packets[packet.packetId];

                MessageSendMode sendMode = MessageSendMode.Reliable;
                Packet basePacket = packet as Packet;
                if (basePacket != null)
                    sendMode = basePacket.sendMode;

                Message message = Message.Create(sendMode, packet.packetId);

                foreach (var prop in packetRef.properties)
                {
                    if (prop.PropertyType.IsEnum)
                    {
                        currentPropName = prop.Name;
                        message.AddInt(Convert.ToInt32(prop.GetValue(packet, null)));
                    }
                    else if (prop.PropertyType == typeof(ushort))
                    {
                        currentPropName = prop.Name;
                        message.AddUShort((ushort)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        currentPropName = prop.Name;
                        message.AddBool((bool)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        currentPropName = prop.Name;
                        message.AddInt((int)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        currentPropName = prop.Name;
                        message.AddString((string)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(float))
                    {
                        currentPropName = prop.Name;
                        message.AddFloat((float)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        currentPropName = prop.Name;
                        message.AddDouble((double)prop.GetValue(packet, null));
                    }
                    else if (prop.PropertyType == typeof(byte[]))
                    {
                        currentPropName = prop.Name;
                        byte[] bytes = (byte[])prop.GetValue(packet, null);
                        message.AddBytes(bytes, true);
                    }
                    else if (prop.PropertyType == typeof(List<string>))
                    {
                        currentPropName = prop.Name;
                        List<string> list = (List<string>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                            message.AddString(item);
                    }
                    else if (prop.PropertyType == typeof(List<bool>))
                    {
                        currentPropName = prop.Name;
                        List<bool> list = (List<bool>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                            message.AddBool(item);
                    }
                    else if (prop.PropertyType == typeof(List<ushort>))
                    {
                        currentPropName = prop.Name;
                        List<ushort> list = (List<ushort>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                            message.AddUShort(item);
                    }
                    else if (prop.PropertyType == typeof(List<int>))
                    {
                        currentPropName = prop.Name;
                        List<int> list = (List<int>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                            message.AddInt(item);
                    }

                    else if (prop.PropertyType == typeof(List<Guid>))
                    {
                        currentPropName = prop.Name;
                        List<Guid> list = (List<Guid>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                            message.AddBytes(item.ToByteArray(), true);
                    }
                    else if (prop.PropertyType == typeof(List<Vector3>))
                    {
                        currentPropName = prop.Name;
                        List<Vector3> list = (List<Vector3>)prop.GetValue(packet, null);
                        message.AddInt(list.Count);
                        foreach (var item in list)
                        {
                            message.AddFloat(item.x);
                            message.AddFloat(item.y);
                            message.AddFloat(item.z);
                        }
                    }

                    else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        currentPropName = prop.Name;
                        Type[] argumentTypes = prop.PropertyType.GetGenericArguments();
                        Type keyType = argumentTypes[0];
                        Type valueType = argumentTypes[1];

                        object dictionary = prop.GetValue(packet, null);

                        int count = (int)dictionary.GetType().GetProperty("Count").GetValue(dictionary, null);

                        var enumerator = ((IEnumerable)dictionary).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            object key = enumerator.Current.GetType().GetProperty("Key").GetValue(enumerator.Current, null);
                            object value = enumerator.Current.GetType().GetProperty("Value").GetValue(enumerator.Current, null);

                            Main.helper.Log($"Key: {key.GetType()}, Value: {value.GetType()}");
                        }
                    }
                    else if (prop.PropertyType == typeof(Vector3))
                    {
                        currentPropName = prop.Name;
                        Vector3 vector = (Vector3)prop.GetValue(packet, null);
                        message.AddFloat(vector.x);
                        message.AddFloat(vector.y);
                        message.AddFloat(vector.z);
                    }
                    else if (prop.PropertyType == typeof(Quaternion))
                    {
                        currentPropName = prop.Name;
                        Quaternion quaternion = (Quaternion)prop.GetValue(packet, null);
                        message.AddFloat(quaternion.x);
                        message.AddFloat(quaternion.y);
                        message.AddFloat(quaternion.z);
                        message.AddFloat(quaternion.w);
                    }
                    else if (prop.PropertyType == typeof(Guid))
                    {
                        currentPropName = prop.Name;
                        Guid guid = (Guid)prop.GetValue(packet, null);
                        message.AddBytes(guid.ToByteArray());
                    }
                    else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        currentPropName = prop.Name;

                        Type itemType = prop.PropertyType.GetGenericArguments()[0];

                        var list = prop.GetValue(packet, null) as System.Collections.IList;
                        if (list != null)
                        {
                            message.AddInt(list.Count); 

                            foreach (var item in list)
                            {
                                if (itemType.IsClass && itemType != typeof(string) || itemType.IsValueType && !itemType.IsPrimitive)
                                {
                                    var fields = itemType.GetFields(); // Get fields
                                    Array.Sort(fields, (x, y) => String.Compare(x.Name, y.Name));
                                    var properties = itemType.GetProperties(); // Get properties
                                    Array.Sort(properties, (x, y) => String.Compare(x.Name, y.Name));


                                    // Serialize fields
                                    foreach (var field in fields)
                                    {
                                        var fieldValue = field.GetValue(item);
                                        AddDynamic(message, fieldValue);
                                    }

                                    // Serialize properties
                                    foreach (var property in properties)
                                    {
                                        var propertyValue = property.GetValue(item);
                                        AddDynamic(message, propertyValue);
                                    }
                                }
                                else
                                {
                                    AddDynamic(message, item);
                                }
                            }
                        }
                    }
                    // You can add more types as needed
                }


                return message;
            }
            catch (Exception ex)
            {
                Main.helper.Log($"Failed to serialise packet {packet.packetId} {packet.GetType().Name} at {currentPropName}");

                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Main.helper.Log("----------------------- Inner exception -----------------------");
                    Main.helper.Log(ex.InnerException.ToString());
                    Main.helper.Log("----------------------- Inner message -----------------------");
                    Main.helper.Log(ex.InnerException.Message);
                    Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                    Main.helper.Log(ex.InnerException.StackTrace);
                }
            }
            return null;
        }

        static void AddDynamic(Message message, object value)
        {
            if (value is int intValue)
                message.AddInt(intValue);
            else if (value is string stringValue)
                message.AddString(stringValue);
            else if (value is bool boolValue)
                message.AddBool(boolValue);
            else if (value is float floatValue)
                message.AddFloat(floatValue);
            else if (value is double doubleValue)
                message.AddDouble(doubleValue);
            else if (value is Vector3 vector)
            {
                message.AddFloat(vector.x);
                message.AddFloat(vector.y);
                message.AddFloat(vector.z);
            }
            else if (value is Quaternion quaternion)
            {
                message.AddFloat(quaternion.x);
                message.AddFloat(quaternion.y);
                message.AddFloat(quaternion.z);
                message.AddFloat(quaternion.w);
            }
            else if (value is Guid guid)
                message.AddBytes(guid.ToByteArray());
            // Add more type checks as necessary
            else
                throw new NotImplementedException($"Type {value.GetType()} serialization not implemented.");
        }


        public static IPacket DeserialisePacket(MessageReceivedEventArgs messageReceived)
        {
            try
            {
                var message = messageReceived.Message;
                var packetRef = Packets[messageReceived.MessageId];
                IPacket p = (IPacket)Activator.CreateInstance(packetRef.packet.GetType());


                foreach (var prop in packetRef.properties)
                {
                    if (prop.PropertyType.IsEnum)
                    {
                        int enumValue = message.GetInt();
                        prop.SetValue(p, Enum.ToObject(prop.PropertyType, enumValue));
                    }
                    else if (prop.PropertyType == typeof(ushort))
                    {
                        prop.SetValue(p, message.GetUShort());
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(p, message.GetBool());
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(p, message.GetInt());
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(p, message.GetString());
                    }
                    else if (prop.PropertyType == typeof(float))
                    {
                        prop.SetValue(p, message.GetFloat());
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        prop.SetValue(p, message.GetDouble());
                    }
                    else if (prop.PropertyType == typeof(byte[]))
                    {
                        byte[] bytes = message.GetBytes();

                        prop.SetValue(p, bytes);
                    }
                    else if (prop.PropertyType == typeof(List<string>))
                    {
                        int count = message.GetInt();
                        List<string> list = new List<string>();

                        for (int i = 0; i < count; i++)
                            list.Add(message.GetString());

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType == typeof(List<bool>))
                    {
                        int count = message.GetInt();
                        List<bool> list = new List<bool>();

                        for (int i = 0; i < count; i++)
                            list.Add(message.GetBool());

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType == typeof(List<ushort>))
                    {
                        int count = message.GetInt();
                        List<ushort> list = new List<ushort>();

                        for (int i = 0; i < count; i++)
                            list.Add(message.GetUShort());

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType == typeof(List<int>))
                    {
                        int count = message.GetInt();
                        List<int> list = new List<int>();

                        for (int i = 0; i < count; i++)
                            list.Add(message.GetInt());

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType == typeof(List<Guid>))
                    {
                        int count = message.GetInt();
                        List<Guid> list = new List<Guid>();

                        for (int i = 0; i < count; i++)
                            list.Add(new Guid(message.GetBytes()));

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType == typeof(List<Vector3>))
                    {
                        int count = message.GetInt();
                        List<Vector3> list = new List<Vector3>();

                        for (int i = 0; i < count; i++)
                        {
                            Vector3 vector = new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
                            list.Add(vector);
                        }

                        prop.SetValue(p, list);
                    }
                    else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        IDictionary dictionary = (IDictionary)prop.GetValue(p, null);
                        Type[] argumentTypes = prop.PropertyType.GetGenericArguments();
                        Type keyType = argumentTypes[0];
                        Type valueType = argumentTypes[1];


                        message.AddInt(dictionary.Count);

                        foreach (DictionaryEntry entry in dictionary)
                        {

                            //Serialize(entry.Key, message); // Implement this method based on the type of 'Key'
                            //Serialize(entry.Value, message); // Implement this method based on the type of 'Value'
                        }
                    }
                    else if (prop.PropertyType == typeof(Vector3))
                    {
                        Vector3 vector = new Vector3(message.GetFloat(), message.GetFloat(), message.GetFloat());
                        prop.SetValue(p, vector);
                    }
                    else if (prop.PropertyType == typeof(Quaternion))
                    {
                        Quaternion quaternion = new Quaternion(message.GetFloat(), message.GetFloat(), message.GetFloat(), message.GetFloat());
                        prop.SetValue(p, quaternion);
                    }
                    else if (prop.PropertyType == typeof(Guid))
                    {
                        Guid guid = new Guid(message.GetBytes());
                        prop.SetValue(p, guid);
                    }
                    // You can add more types as needed
                }

                if (KCServer.IsRunning)
                {
                    //if (!p.GetType().Name.Contains("Update"))
                    //Main.helper.Log($"Received packet {messageReceived.MessageId} {p.GetType().Name} from {messageReceived.FromConnection.Id}");
                    //Main.helper.Log("Setting packet client id to: " + messageReceived.FromConnection.Id + " for packet: " + p.GetType().Name);
                    //p.clientId = messageReceived.FromConnection.Id;
                }

                return p;
            }
            catch (Exception ex)
            {
                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Main.helper.Log("----------------------- Inner exception -----------------------");
                    Main.helper.Log(ex.InnerException.ToString());
                    Main.helper.Log("----------------------- Inner message -----------------------");
                    Main.helper.Log(ex.InnerException.Message);
                    Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                    Main.helper.Log(ex.InnerException.StackTrace);
                }
            }
            return null;
        }
    }
}
