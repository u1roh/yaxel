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
        //console.log("get(" + func + "): text = " + text);
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
        //console.log("ERROR @ getOr(" + func + ", " + JSON.stringify(defValue) + ") > " + JSON.stringify(result.value));
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

export function fetchModuleList(): Promise<string[]> {
    return getOr<string[]>('modules', []);
}

export function fetchFunctionList(modName: string): Promise<string[]> {
    return getOr<string[]>('modules/' + modName + '/functions', []);
}

export function fetchBreathCount(): Promise<number> {
    return getOr<number>('modules/breath', -1);
}

export function fetchModuleBreathCount(modName: string): Promise<number> {
    return getOr<number>('modules/' + modName + '/breath', -1);
}

export async function fetchUserCode(modName: string): Promise<string> {
    const result = await get<string>('modules/' + modName + '/usercode');
    return result.tag === 'ok' ? result.value : JSON.stringify(result.value);
}

export function updateUserCode(modName: string, code: string): Promise<Result<any>> {
    return postJson('api/modules/' + modName + '/usercode/update', code);
}

export function invokeFunction(modName: string, funcName: string, args: any[]): Promise<Result<any>> {
    console.log("api.invokeFunction(" + funcName + ", " + JSON.stringify(args) + ")");
    return postJson('api/modules/' + modName + '/functions/' + funcName + '/invoke', JSON.stringify(args));
}

export function fetchFunction(modName: string, funcName: string): Promise<Result<yaxel.Fun>> {
    console.log("api.fetchFunction(" + modName + ", " + funcName + ")")
    return get('modules/' + modName + '/functions/' + funcName);
}

export async function fetchModuleFunctions(modName: string): Promise<Result<yaxel.Fun>[]> {
    const names = await fetchFunctionList(modName);
    const funcs = new Array<Result<yaxel.Fun>>(names.length);
    for (let i = 0; i < names.length; ++i) {
        funcs[i] = await fetchFunction(modName, names[i]);
    }
    return funcs;
}
