/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitFilters
{
    public class Commandline
    {
        public class Option
        {
            public char shortname;
            public string longname;
            public bool withvalue;
            public string help;
            public string value;
            public bool set;

            public Option(char shortname, string longname, bool withvalue, string help)
            {
                this.shortname = shortname;
                this.longname = longname;
                this.withvalue = withvalue;
                this.help = help;
                this.value = "";
                this.set = false;
            }
        }

        public static Dictionary<string, Option> MapOptions(Option[] options)
        {
            Dictionary<string, Option> result = new Dictionary<string, Option>();
            foreach (Option opt in options) {
                if (opt.shortname > 0) {
                    result.Add(opt.shortname.ToString(), opt);
                }
                if (string.IsNullOrEmpty(opt.longname) == false) {
                    result.Add(opt.longname, opt);
                }
            }
            return result;
        }

        public static bool Parse(string[] args, Option[] options, out string[] notargs)
        {
            Option last = null;
            List<string> ntarg = new List<string>();
            foreach (string arg in args) {
                if (string.IsNullOrEmpty(arg)) {
                    continue;
                }
                if (arg.Length == 1) {
                    if (arg[0] != '/' && arg[0] != '-') {
                        if (last != null && last.withvalue) {
                            last.value = arg;
                            last = null;
                        } else {
                            ntarg.Add(arg);
                        }
                    }
                    continue;
                }
                if (arg.Length == 2 && arg[0] == '-' && arg[1] == '-') {
                    if (last != null && last.withvalue) {
                        notargs = args;
                        return false;
                    }
                    continue;
                }
                bool shortn = false;
                bool longn = false;
                string name = "";
                if (arg[0] == '/') {
                    shortn = true;
                    longn = true;
                    name = arg.Substring(1);
                } else if (arg[0] == '-') {
                    if (arg[1] == '-') {
                        longn = true;
                        name = arg.Substring(2);
                    } else {
                        shortn = true;
                        name = arg.Substring(1);
                    }
                }
                if (shortn == false && longn == false) {
                    if (last != null && last.withvalue) {
                        last.value = arg;
                        last = null;
                    } else {
                        ntarg.Add(arg);
                    }
                    continue;
                }
                if (last != null && last.withvalue) {
                    notargs = args;
                    return false;
                }
                bool isoption = false;
                for (int x = options.Length - 1; x > -1; x--) {
                    if (shortn) {
                        if (name[0] == options[x].shortname) {
                            options[x].set = true;
                            last = options[x];
                            isoption = true;
                            break;
                        }
                    }
                    if (longn && string.IsNullOrEmpty(options[x].longname) == false) {
                        if (name == options[x].longname) {
                            options[x].set = true;
                            last = options[x];
                            isoption = true;
                            break;
                        }
                    }
                }
                if (isoption == false) {
                    notargs = args;
                    return false;
                }
            }
            if (last != null && last.withvalue) {
                notargs = args;
                return false;
            }
            notargs = ntarg.ToArray();
            return true;
        }

        public static string Print(Option[] options, int maxwidth = 80)
        {
            return PrintArray(options, maxwidth)
                .Aggregate((now, next) => now.Append(Environment.NewLine).Append(next)).ToString();
        }

        public static StringBuilder[] PrintArray(Option[] options, int maxwidth = 80)
        {
            StringBuilder[] result = new StringBuilder[options.Length];
            int maxlong = 0;
            for (int o = 0; o < options.Length; o++) {
                result[o] = new StringBuilder("  ");
                result[o].Append(options[o].shortname > 0 ? "-" + options[o].shortname : "  ");
                result[o].Append(" ");
                result[o].Append(string.IsNullOrEmpty(options[o].longname) ? "" : "--" + options[o].longname);
                if (result[o].Length > maxlong) {
                    maxlong = result[o].Length;
                }
            }
            for (int o = 0; o < options.Length; o++) {
                if (string.IsNullOrEmpty(options[o].help) == false) {
                    List<string> help = new List<string>(options[o].help.Split(new char[] {'\r', '\n'},
                        StringSplitOptions.RemoveEmptyEntries));
                    int width = maxwidth - maxlong - 3;
                    for (int h = 0; h < help.Count; h++) {
                        if (help[h].Length > width) {
                            int split = help[h].LastIndexOf(' ', width);
                            if (split == -1) {
                                split = width;
                            }
                            if (h + 1 < help.Count) {
                                help.Insert(h + 1, help[h].Substring(split).TrimStart());
                            } else {
                                help.Add(help[h].Substring(split).TrimStart());
                            }
                            help[h] = help[h].Substring(0, split);
                        }
                    }
                    result[o].Append(new string(' ', maxwidth - width - result[o].Length));
                    result[o].Append(help[0]);
                    for (int h = 1; h < help.Count; h++) {
                        result[o].AppendLine();
                        result[o].Append(new string(' ', maxwidth - width));
                        result[o].Append(help[h]);
                    }
                }
            }
            return result;
        }
    }
}
