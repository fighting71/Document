查看*MemberwiseClone*的实现

去往上面那个github地址 找到 *clr/src/vm/ecall.cpp*

	FCFuncElement("MemberwiseClone", ObjectNative::Clone)

前往*ObjectNative* 

*clr/src/vm/comobject.cpp*

	FCIMPL1(Object*, ObjectNative::Clone, Object* pThisUNSAFE)
	{
	    CONTRACTL
	    {
	        MODE_COOPERATIVE;
	        DISABLED(GC_TRIGGERS);  // can't use this in an FCALL because we're in forbid gc mode until we setup a H_M_F.
	        THROWS;
	        SO_TOLERANT;
	    }
	    CONTRACTL_END;
	
	    OBJECTREF refClone = NULL;
	    OBJECTREF refThis  = ObjectToOBJECTREF(pThisUNSAFE);
	
	    if (refThis == NULL)
	        FCThrow(kNullReferenceException);
	
	    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_2(Frame::FRAME_ATTR_RETURNOBJ, refClone, refThis);
	
	    // ObjectNative::Clone() ensures that the source and destination are always in
	    // the same context.
	
	    MethodTable* pMT;
	    DWORD cb;
	
	    pMT = refThis->GetMethodTable();
	
	    // assert that String has overloaded the Clone() method
	    _ASSERTE(pMT != g_pStringClass);
	
	    cb = pMT->GetBaseSize() - sizeof(ObjHeader);
	
	
	    if (pMT->IsArray()) {
	
	        BASEARRAYREF base = (BASEARRAYREF)refThis;
	        cb += base->GetNumComponents() * pMT->GetComponentSize();
	
	        refClone = DupArrayForCloning(base);
	    } else {
	        // We don't need to call the <cinit> because we know
	        //  that it has been called....(It was called before this was created)
	        refClone = AllocateObject(pMT);
	    }
	
	    // copy contents of "this" to the clone
	    memcpyGCRefs(OBJECTREFToObject(refClone), OBJECTREFToObject(refThis), cb);
	
	    HELPER_METHOD_FRAME_END();
	        
	    return OBJECTREFToObject(refClone);
	}
	FCIMPLEND