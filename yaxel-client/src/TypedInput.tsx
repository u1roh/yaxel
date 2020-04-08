import React, { useState } from 'react';
import * as yaxel from './yaxel'
import './TypedInput.css';

interface UnionInputProps {
    union: yaxel.UnionType;
    value: any;
    onChange: (value: any) => void;
}

function UnionInput(props: UnionInputProps) {
    const setSelectedIndex = (i: number) => {
        props.onChange({
            name: props.union.cases[i].name,
            value: yaxel.defaultValueOf(props.union.cases[i].type)
        });
    }
    return (<div>
        <select onChange={(e) => setSelectedIndex(e.target.selectedIndex)} value={props.value ? props.value["name"] : "(undefined)"}>
            {props.union.cases.map(item =>
                <option value={item.name}>{item.name}</option>)}
        </select>
        {
            (() => {
                if (props.value) {
                    const item = props.union.cases.find(item => item.name === props.value["name"]);
                    return item !== undefined ?
                        <TypedInput name='' type={item.type} value={props.value["value"]}
                            onChange={x => {
                                const obj = props.value;
                                obj.value = x;
                                props.onChange(obj);
                            }} />
                        : <span></span>;
                } else {
                    return <span></span>;
                }
            })()
        }
    </div>);
}

function InputNumber(props: { value: number, type: 'int' | 'float', onChange: (input: number) => void }) {
    const [text, setText] = useState(props.value.toString());
    const onInput = (input: string) => {
        const value = props.type === 'int' ? Number.parseInt(input) : Number.parseFloat(input);
        if (Number.isNaN(value)) {
            console.log("invalid input: " + input);
        } else {
            setText(value.toString());
            props.onChange(value);
        }
    };
    return <input type='number' value={text}
        onChange={e => setText(e.target.value)}
        onKeyDown={e => { if (e.key === 'Enter') onInput(text); }}
        onBlur={() => onInput(text)}></input>;
}

interface TypedInputProps {
    name: string;
    type: yaxel.Type;
    value: any;
    onChange: (value: any) => void;
}

function TypedInput(props: TypedInputProps) {
    const caption = props.name.length === 0 ? "" : props.name + " = ";
    const onChange = (x: any) => {
        console.log("TypedInput#onChange: " + x);
        props.onChange(x);
    };
    const render = () => {
        switch (props.type) {
            case null:
                return <span></span>;
            case "int":
                return <span>{caption}
                    <InputNumber type='int' value={props.value} onChange={onChange} />
                </span>;
            case "float":
                return <span>{caption}
                    <InputNumber type='float' value={props.value} onChange={onChange} />
                </span>;
            case "string":
                return <span>{caption}
                    <input value={props.value} onChange={(e) => onChange(e.target.value)}></input>
                </span>;
            case "bool":
                return <span>
                    <input type='checkbox' checked={props.value} onChange={(e) => onChange(e.target.checked)}></input>
                    <label>{props.name}</label>
                </span>;
            default:
                if (Array.isArray(props.type)) {
                    return JSON.stringify(props.type);
                }
                switch (props.type.tag) {
                    case 'record':
                        const onFieldChange = (name: string, value: any) => {
                            console.log("record / onChange: name = " + name + ", value = " + JSON.stringify(value));
                            const record = props.value;
                            record[name] = value;
                            onChange(record);
                        };
                        return (<div>
                            <div>{caption}{props.type.name}</div>
                            {props.type.fields.map(item => {
                                try {
                                    return <div><TypedInput name={item.name} type={item.type} value={props.value[item.name]} onChange={(x) => onFieldChange(item.name, x)} /></div>;
                                }
                                catch (e) {
                                    return <div>{JSON.stringify(e)}</div>;
                                }
                            })}
                        </div>);
                    case 'union':
                        return <div>{caption}<UnionInput union={props.type} value={props.value} onChange={x => onChange(x)} /></div>;
                    default:
                        return JSON.stringify(props.type);
                }
        }
    };
    return <div className="TypedInput">{render()}</div>;
}

export default TypedInput;
