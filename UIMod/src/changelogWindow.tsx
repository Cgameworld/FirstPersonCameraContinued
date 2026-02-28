import React, { useState, useRef, useCallback } from 'react';
import { trigger } from "cs2/api";
import dynamiclineImg from "images/dynamicline.jpg";
import pipwindowImg from "images/pipwindow.jpg";

// ---- EDIT CHANGELOG CONTENT HERE ----
const CHANGELOG_TITLE = "v1.6 Update!";

interface ChangelogItem {
    text: string;
    image?: string;
    imageWidth?: string;
    imageHeight?: string;
}

interface ChangelogSection {
    heading: string;
    items: ChangelogItem[];
}

const CHANGELOG_HIGHLIGHTS: ChangelogSection[] = [
    { heading: "Main New Features:", items: [
        { text: "Added dynamic strip map stop display when following transit vehicles", image: dynamiclineImg, imageWidth: '640rem', imageHeight: '77rem' },
        { text: "Added toggleable picture-in-picture overlay (press p)", image: pipwindowImg, imageWidth: '640rem', imageHeight: '144rem' },
        { text: "Added zoom mode (press z)" },
    ]},
];

const CHANGELOG_FULL: ChangelogSection[] = [
    { heading: "Improvements:", items: [
        { text: "Fixed lag when looking at ground" },
        { text: "Free Camera view now stays above water" },
        { text: "Follow Random Bicycle mode no longer picks parked bikes" },
    ]},
    { heading: "Other Changes:", items: [
        { text: "Vehicle type hidden on default in infobox" },
        { text: "Increased transition speed factor speed default" },
        { text: "Removed first person shortcut when line info panel is selected" },
    ]},
];
// ---- END CHANGELOG CONTENT ----

const SectionList: React.FC<{ sections: ChangelogSection[] }> = ({ sections }) => (
    <>
        {sections.map((section, i) => (
            <div key={i} style={i > 0 ? { marginTop: '12rem' } : {}}>
                <p className="p_CKq" style={{ fontWeight: 'bold', marginBottom: '6rem', fontSize: '19rem' }}>{section.heading}</p>
                {section.items.map((item, j) => (
                    <div key={j}>
                        <p className="p_CKq" style={{ fontSize: '17rem' }}>- {item.text}</p>
                        {item.image && (
                            <img
                                src={item.image}
                                style={{
                                    width: item.imageWidth,
                                    height: item.imageHeight,
                                    marginTop: '6rem',
                                    marginBottom: '8rem',
                                }}
                            />
                        )}
                    </div>
                ))}
            </div>
        ))}
    </>
);

const ChangelogWindow: React.FC = () => {
    const [showMore, setShowMore] = useState(false);
    const [visible, setVisible] = useState(true);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const dragRef = useRef<{ startX: number; startY: number; origX: number; origY: number } | null>(null);

    const onClose = () => {
        setVisible(false);
        trigger("fpc", "DismissChangelog");
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

    if (!visible) return null;

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
                    width: '672rem',
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
                        <div className="title_SVH title_zQN">First Person Camera Continued</div>
                        <button className="button_bvQ button_bvQ close-button_wKK" onClick={onClose}>
                            <div className="tinted-icon_iKo icon_PhD" style={{ maskImage: "url(Media/Glyphs/Close.svg)" }}></div>
                        </button>
                    </div>
                </div>
                <div className="content_VBF content_AD7 child-opacity-transition_nkS">
                    <div className="icon-layout_cZT row_L6K">
                        <div className="main-column_Jzk">
                            <div className="error-message_r4_" style={{marginTop: '-4rem'}}>
                                <div className="paragraphs_nbD" style={{ padding: '8rem' }}>
                                    <p className="p_CKq" style={{ fontSize: '21rem', fontWeight: 'bold', marginBottom: '12rem' }}>{CHANGELOG_TITLE}</p>
                                    <SectionList sections={CHANGELOG_HIGHLIGHTS} />

                                    {!showMore && (
                                        <div style={{ marginTop: '14rem' }}>
                                            <button
                                                className="button_HeP button_gJo"
                                                style={{ width: 'auto', padding: '5rem 14rem'}}
                                                onClick={() => setShowMore(true)}
                                            >
                                                Show Full Changelog ▼
                                            </button>
                                        </div>
                                    )}

                                    {showMore && (
                                        <>
                                            <div style={{ marginTop: '14rem' }}>
                                                <button
                                                    className="button_HeP button_gJo"
                                                    style={{ width: 'auto', padding: '5rem 14rem'}}
                                                    onClick={() => setShowMore(false)}
                                                >
                                                    Hide Full Changelog ▲
                                                </button>
                                            </div>
                                            <div style={{ marginTop: '12rem' }}>
                                                <SectionList sections={CHANGELOG_FULL} />
                                            </div>
                                        </>
                                    )}
                                </div>
                            </div>
                            <div className="buttons-container" style={{ marginTop: '22rem', marginRight: '12rem', textAlign: 'right' }}>
                                <div className="buttons_lZi row_L6K" style={{ width: '175rem' }}>
                                    <button className="button_HeP button_gJo" style={{ width: "130rem"}} onClick={onClose}>Ok</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ChangelogWindow;
