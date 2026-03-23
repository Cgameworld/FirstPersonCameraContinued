import React, { useState, useRef, useCallback, useEffect } from 'react';
import { bindValue, trigger, useValue } from "cs2/api";
import engine from 'cohtml/cohtml';
import ErrorPopup from './errorPopup';

interface CategoryItem {
    key: string;
    label: string;
}

interface Category {
    name: string;
    items: CategoryItem[];
}

const RandomFollowCategories$ = bindValue<string>('fpc', 'RandomFollowCategories', '[]');
const NoEntitiesError$ = bindValue<string>('fpc', 'NoEntitiesError', '');
const IsEntered$ = bindValue<boolean>('fpc', 'IsEntered');

function initSelections(categories: Category[]): Record<string, boolean> {
    const selections: Record<string, boolean> = {};
    categories.forEach(cat => cat.items.forEach(item => {
        selections[item.key] = false;
    }));
    return selections;
}

const RandomFollowWindow: React.FC<{ onClose: () => void }> = ({ onClose }) => {
    const categoriesJson = useValue(RandomFollowCategories$);
    const categories: Category[] = React.useMemo(() => {
        try { return JSON.parse(categoriesJson); }
        catch { return []; }
    }, [categoriesJson]);

    const [selections, setSelections] = useState<Record<string, boolean>>(() => initSelections(categories));
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const dragRef = useRef<{ startX: number; startY: number; origX: number; origY: number } | null>(null);

    const toggle = (key: string) => {
        setSelections(prev => {
            return { ...prev, [key]: !prev[key] };
        });
    };

    const toggleCategory = (cat: Category) => {
        const allChecked = cat.items.every(item => selections[item.key]);
        setSelections(prev => {
            const updated = { ...prev };
            cat.items.forEach(item => { updated[item.key] = !allChecked; });
            return updated;
        });
    };

    const noEntitiesError = useValue(NoEntitiesError$);
    const isEntered = useValue(IsEntered$);

    useEffect(() => {
        if (isEntered) onClose();
    }, [isEntered]);

    const onStart = () => {
        const selectedEntries = Object.entries(selections).filter(([_, v]) => v);
        if (selectedEntries.length === 0) return;

        const selectedKeys = selectedEntries.map(([k]) => k).join(',');

        const labelLookup: Record<string, string> = {};
        categories.forEach(cat => cat.items.forEach(item => {
            labelLookup[item.key] = item.label;
        }));
        const selectedLabels = selectedEntries.map(([k]) => labelLookup[k] || k).join(', ');

        engine.trigger("audio.playSound", "select-item", 1);
        trigger("fpc", "FilteredRandomFPC", selectedKeys + '|' + selectedLabels);
    };

    const onMouseDown = useCallback((e: React.MouseEvent) => {
        dragRef.current = {
            startX: e.clientX,
            startY: e.clientY,
            origX: position.x,
            origY: position.y,
        };

        const onMouseMove = (ev: MouseEvent) => {
            if (!dragRef.current) return;
            setPosition({
                x: dragRef.current.origX + (ev.clientX - dragRef.current.startX),
                y: dragRef.current.origY + (ev.clientY - dragRef.current.startY),
            });
        };

        const onMouseUp = () => {
            dragRef.current = null;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }, [position]);

    if (categories.length === 0) return null;

    return (
        <div
            style={{
                position: "fixed",
                top: 0,
                left: 0,
                width: "100%",
                height: "85%",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                zIndex: 9999,
                pointerEvents: "none",
            }}
        >
            <div
                className="panel_YqS error-dialog_iaV"
                style={{
                    maxWidth: "100%",
                    maxHeight: "100%",
                    width: '900rem',
                    pointerEvents: "auto",
                    transform: `translate(${position.x}px, ${position.y}px)`,
                }}
            >
                <div
                    className="header_jAe header_Bpo child-opacity-transition_nkS"
                    onMouseDown={onMouseDown}
                    style={{ cursor: 'grab' }}
                >
                    <div className="title-bar_PF4">
                        <div className="icon_VQU">
                            <img className="iconImg_ThV" src="coui://uil/Standard/VideoCamera.svg" />
                        </div>
                        <div className="icon-space_h_f"></div>
                        <div className="title_SVH title_zQN">Follow Random</div>
                        <button className="button_bvQ button_bvQ close-button_wKK" onClick={onClose}>
                            <div className="tinted-icon_iKo icon_PhD" style={{ maskImage: "url(Media/Glyphs/Close.svg)" }}></div>
                        </button>
                    </div>
                </div>
                <div className="content_VBF content_AD7 child-opacity-transition_nkS">
                    <div className="icon-layout_cZT row_L6K">
                        <div className="main-column_Jzk">
                            <div className="error-message_r4_" style={{ marginTop: '-4rem' }}>
                                <div className="fpc-random-follow-grid" style={{
                                    display: 'flex',
                                    flexWrap: 'wrap',
                                    gap: '8rem',
                                    padding: '8rem',
                                }}>
                                    {categories.map((cat, catIdx) => (
                                        <div className="statistics-category-item_qVI" key={catIdx} style={{
                                            width: '260rem',
                                            flexShrink: 0,
                                        }}>
                                            <div
                                                className="header_Ld7"
                                                onClick={() => toggleCategory(cat)}
                                                style={{ cursor: 'pointer' }}
                                            >
                                                {cat.name}
                                            </div>
                                            <div className="items_AIY">
                                                {cat.items.map(item => (
                                                    <div
                                                        className="foldout-item_dah foldout-item_wOF disable-mouse-states_js5"
                                                        key={item.key}
                                                        onClick={() => toggle(item.key)}
                                                        style={{ cursor: 'pointer' }}
                                                    >
                                                        <div className="header_MP_ header_8H_ item-mouse-states_Fmi item-focused_FuT">
                                                            <div className="header-content_SqG header-content_wUX">
                                                                <div className={`toggle_GGm toggle_cca item-mouse-states_Fmi icon_xRc ${selections[item.key] ? 'checked' : 'unchecked'}`}>
                                                                    <div className={`checkmark_NXV ${selections[item.key] ? 'checked' : ''}`}></div>
                                                                </div>
                                                                <div className="label_VRN">{item.label}</div>
                                                            </div>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                            <div className="buttons-container" style={{ marginTop: '22rem', marginRight: '12rem', textAlign: 'right' }}>
                                <div className="buttons_lZi row_L6K" style={{ width: '175rem' }}>
                                    <button className="button_HeP button_gJo" style={{ width: "130rem" }} onClick={onStart}>START</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            {noEntitiesError && <ErrorPopup />}
        </div>
    );
};

export default RandomFollowWindow;
