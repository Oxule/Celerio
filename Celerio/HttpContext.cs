namespace Celerio;

public class HttpContext
{
    public class Request
    {
        public enum HttpMethod
        {
            POST,
            GET,
            PATCH,
            DELETE,
            UPDATE,
            PUT,
            OPTIONS,
            TRACE,
            UNKNOWN
        }
        public HttpMethod Method;
        public string URI = "/";
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public string? Body;

        public Request()
        {
        }

        private struct TaggedZone
        {
            public byte Tag;
            public int Start;
            public int End;

            public TaggedZone(byte tag, int start, int end)
            {
                Tag = tag;
                Start = start;
                End = end;
            }
        }

        public static bool TryParse(string text, out Request request)
        {
            request = new Request();

            var l = text.Split('\n');

            int pointer = 0;
            
            for (int i = 0; i < l.Length; i++)
            {

                pointer += l[i].Length;
            }
            
            return true;
        }
        
        //TODO: Remove
        private static bool TryParseLegacy(Stream stream, out Request req)
        {
            req = new Request();
            
            byte state = 0;
            
            var reader = new StreamReader(stream);
            
            char[] buffer = new char[stream.Length];

            List<TaggedZone> zone = new List<TaggedZone>();
            
            int pointer_a = 0;
            int pointer_b = 0;
            
            while (true)
            {
                var b = reader.Read();
                if (b == -1)
                    break;

                var с = (char)b;
                
                switch (state)
                {
                    case 0:
                        if (с == ' ')
                        {
                            zone.Add(new TaggedZone(0, pointer_b, pointer_a-1));
                            pointer_b = pointer_a + 1;
                            state = 1;
                        }
                        break;
                    case 1:
                        if (с == ' ')
                        {
                            zone.Add(new TaggedZone(0, pointer_b, pointer_a-1));
                            pointer_b = pointer_a + 1;
                            state = 1;
                        }
                        break;
                }
                
                pointer_a++;
            }
            
            return true;
        }
    }

    public class Response
    {
        
    }
}