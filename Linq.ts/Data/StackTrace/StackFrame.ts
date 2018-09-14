﻿namespace TsLinq {

    export class StackFrame {

        public caller: string;
        public file: string;
        public memberName: string;
        public line: number;
        public column: number;

        public toString(): string {
            return `${this.caller} [as ${this.memberName}](${this.file}:${this.line}:${this.column})`;
        }

        public static Parse(line: string): StackFrame {
            var frame: StackFrame = new StackFrame();
            var file: string = StackFrame.getFileName(line);
            var caller = line.replace(file, "").trim().substr(3);
            console.log(file);
            file = file.substr(1, file.length - 2);

            var position: string = file.match(/([:]\d+){2}$/m)[0];
            var location = From(position.split(":"))
                .Where(s => s.length > 0)
                .Select(parseInt)
                .ToArray();

            frame.file = file.substr(0, file.length - position.length);

            var alias: RegExpMatchArray = caller.match(/\[.+\]/);
            var memberName = (!alias || alias.length == 0) ? null : alias[0];

            if (memberName) {
                caller = caller
                    .substr(0, caller.length - memberName.length)
                    .trim();
                frame.memberName = memberName
                    .substr(3, memberName.length - 4)
                    .trim();
            } else {
                var t = caller.split(".");
                frame.memberName = t[t.length - 1];
            }

            frame.caller = caller;
            frame.line = location[0];
            frame.column = location[1];

            return frame;
        }

        private static getFileName(line: string): string {
            var matches = line.match(/\(.+\)/);

            if (!matches || matches.length == 0) {
                // 2018-09-14 可能是html文件之中
                return `(${line.substr(6).trim()})`;
            } else {
                return matches[0];
            }
        }
    }
}