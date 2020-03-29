import * as yaxel from './yaxel'

export interface Ok<T> {
    tag: "ok";
    value: T;
}
export interface Err {
    tag: "err";
    value: any;
}

export type Result<T> = Ok<T> | Err;

async function postJson<T>(input: RequestInfo, json: string): Promise<Result<T>> {
    const res = await fetch(input, {
        method: 'POST',
        body: json,
        headers: {
            'Content-Type': 'application/json'
        }
    });
    const text = await res.text();
    const result = JSON.parse(text);
    return result.name === "Ok" ? { tag: 'ok', value: result.value } : { tag: 'err', value: result.value }
}

async function get<T>(func: string): Promise<Result<T>> {
    try {
        const res = await fetch('api/' + func);
        const text = await res.text();
        console.log("get(" + func + "): text = " + text);
        const json = JSON.parse(text);
        return json.name === "Ok" ? { tag: 'ok', value: json.value } : { tag: 'err', value: json.value }
    }
    catch (e) {
        console.log(e);
        return { tag: 'err', value: e };
    }
}

async function getOr<T>(func: string, defValue: T): Promise<T> {
    const result = await get<T>(func);
    if (result.tag === 'err') {
        console.log("ERROR @ getOr(" + func + ", " + JSON.stringify(defValue) + ") > " + JSON.stringify(result.value));
        return defValue;
    } else {
        return result.value;
    }
}

async function fetchText(input: RequestInfo): Promise<string> {
    const res = await fetch(input);
    return res.text();
}

async function fetchBy<T>(input: RequestInfo, map: (text: string) => T): Promise<T> {
    const text = await fetchText(input);
    return map(text);
}

export function fetchFunctionList(): Promise<string[]> {
    return getOr<string[]>('function', []);
}

export function fetchBreathCount(): Promise<number> {
    return getOr<number>('breath', -1);
}

export async function fetchUserCode(): Promise<string> {
    const result = await get<string>('usercode');
    return result.tag == 'ok' ? result.value : JSON.stringify(result.value);
}

export function updateUserCode(code: string): Promise<Result<any>> {
    return postJson('api/update-usercode', code);
}

export function invokeFunction(funcName: string, args: any[]): Promise<Result<any>> {
    console.log("api.invokeFunction(" + funcName + ", " + JSON.stringify(args) + ")");
    return postJson('api/invoke/' + funcName, JSON.stringify(args));
}

export function fetchFunction(funcName: string): Promise<Result<yaxel.Fun>> {
    console.log("api.fetchFunction(" + funcName + ")")
    return get('function/' + funcName);
}
